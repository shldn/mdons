using UnityEngine;
using System.Collections;

public class ConfettiController : MonoBehaviour {

    ParticleEmitter particleEmitter = null;
    Color[] randomColors = new Color[] { Color.red, Color.blue, Color.green, Color.magenta, Color.yellow };

    public bool Emit { get { return (particleEmitter != null) ? particleEmitter.emit : false; } set { if(particleEmitter != null) particleEmitter.emit = value; } }

    void Start()
    {
        particleEmitter = gameObject.GetComponent<ParticleEmitter>();
    }

    void LateUpdate()
    {
        if (particleEmitter.emit)
        {
            // Randomize confetti colors, fade out as they lose energy.
            Particle[] particles = particleEmitter.particles;
            for (int i = 0; i < particles.Length; i++)
            {
                Color particleColor = particles[i].color;
                if (Random.Range(0f, 1f) <= 0.1)
                    particleColor = randomColors[Random.Range(0, randomColors.Length)];
                particleColor.a = particles[i].energy / particles[i].startEnergy;
                particles[i].color = particleColor;
            }
            particleEmitter.particles = particles;
        }
    }
}
