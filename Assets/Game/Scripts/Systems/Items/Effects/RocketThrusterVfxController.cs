using System.Collections;
using UnityEngine;

public class RocketThrusterVfxController : MonoBehaviour
{
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem startPS;
    [SerializeField] private ParticleSystem loopPS;
    [SerializeField] private ParticleSystem stopPS;

    [Header("Timing")]
    [SerializeField] private float startToLoopDelay = 0.12f;

    private Coroutine transitionRoutine;
    private bool isThrusting;

    public bool IsThrusting => isThrusting;

    private void Awake()
    {
        StopParticle(startPS, ParticleSystemStopBehavior.StopEmittingAndClear);
        StopParticle(loopPS, ParticleSystemStopBehavior.StopEmittingAndClear);
        StopParticle(stopPS, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void SetThrustActive(bool active)
    {
        if (active)
        {
            if (isThrusting) return;

            isThrusting = true;
            RestartTransition(TurnOnRoutine());
            return;
        }

        if (!isThrusting) return;

        isThrusting = false;
        StopTransition();

        if (loopPS != null)
            loopPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (stopPS != null)
            stopPS.Play(true);
    }

    public void ForceStopAll(bool clear = true)
    {
        isThrusting = false;
        StopTransition();

        ParticleSystemStopBehavior behavior = clear
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;

        StopParticle(startPS, behavior);
        StopParticle(loopPS, behavior);
        StopParticle(stopPS, behavior);
    }

    private IEnumerator TurnOnRoutine()
    {
        StopParticle(stopPS, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (startPS != null)
            startPS.Play(true);

        if (startToLoopDelay > 0f)
            yield return new WaitForSeconds(startToLoopDelay);

        if (!isThrusting) yield break;

        if (loopPS != null)
            loopPS.Play(true);
    }

    private void RestartTransition(IEnumerator routine)
    {
        StopTransition();
        transitionRoutine = StartCoroutine(routine);
    }

    private void StopTransition()
    {
        if (transitionRoutine == null) return;
        StopCoroutine(transitionRoutine);
        transitionRoutine = null;
    }

    private static void StopParticle(ParticleSystem ps, ParticleSystemStopBehavior behavior)
    {
        if (ps == null) return;
        ps.Stop(true, behavior);
    }
}
