import csv
import os
import re
import sys
import shutil

HEX32 = re.compile(r"^[0-9a-f]{32}$", re.IGNORECASE)

def norm_path(p: str) -> str:
    p = p.strip().strip("\ufeff")  # BOM
    p = p.replace("\\", "/")
    return p

def read_csv_map(csv_path: str):
    m = {}
    with open(csv_path, "r", encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        # 兼容：如果没有表头就按两列读
        if reader.fieldnames and "path" in reader.fieldnames and "guid" in reader.fieldnames:
            for row in reader:
                path = norm_path(row["path"].strip().strip('"'))
                guid = row["guid"].strip()
                if path and guid:
                    m[path] = guid
        else:
            f.seek(0)
            r = csv.reader(f)
            for row in r:
                if len(row) < 2:
                    continue
                path = norm_path(row[0].strip().strip('"'))
                guid = row[1].strip()
                if path.lower() == "path" and guid.lower() == "guid":
                    continue
                if path and guid:
                    m[path] = guid
    return m

def fix_one_meta(meta_path: str, new_guid: str) -> bool:
    # 备份一次
    bak_path = meta_path + ".bak"
    if not os.path.exists(bak_path):
        shutil.copy2(meta_path, bak_path)

    with open(meta_path, "r", encoding="utf-8", errors="replace") as f:
        lines = f.readlines()

    changed = False
    out_lines = []
    guid_written = False

    for line in lines:
        if line.startswith("guid:"):
            out_lines.append(f"guid: {new_guid}\n")
            guid_written = True
            changed = True
        else:
            out_lines.append(line)

    if not guid_written:
        # 没有 guid 行就插入（通常在 fileFormatVersion 后面）
        inserted = False
        tmp = []
        for line in out_lines:
            tmp.append(line)
            if (not inserted) and line.startswith("fileFormatVersion:"):
                tmp.append(f"guid: {new_guid}\n")
                inserted = True
                changed = True
        out_lines = tmp if inserted else [f"fileFormatVersion: 2\n", f"guid: {new_guid}\n"] + out_lines
        changed = True

    if changed:
        with open(meta_path, "w", encoding="utf-8", newline="") as f:
            f.writelines(out_lines)
    return changed

def main():
    if len(sys.argv) < 2:
        print("Usage: python fix_meta_guids.py <guid_map_csv>")
        sys.exit(1)

    csv_path = sys.argv[1]
    mapping = read_csv_map(csv_path)

    project_root = os.getcwd()
    fixed = 0
    missing_meta = 0
    bad_guid = 0

    for asset_path, guid in mapping.items():
        asset_path = norm_path(asset_path)
        if not asset_path.startswith("Assets/"):
            # 你说只换 Assets，这里就忽略其他（Packages/ 等）
            continue

        if not HEX32.match(guid):
            bad_guid += 1
            print(f"[SKIP] GUID not hex32: {asset_path} -> {guid}")
            continue

        abs_asset = os.path.join(project_root, asset_path.replace("/", os.sep))
        meta_path = abs_asset + ".meta"

        if not os.path.exists(meta_path):
            missing_meta += 1
            print(f"[MISS] meta not found: {meta_path}")
            continue

        if fix_one_meta(meta_path, guid):
            fixed += 1

    print("\n==== DONE ====")
    print(f"Fixed metas: {fixed}")
    print(f"Missing metas: {missing_meta}")
    print(f"Bad GUID in CSV: {bad_guid}")
    print("Note: .bak backups were created next to each modified .meta")

if __name__ == "__main__":
    main()
