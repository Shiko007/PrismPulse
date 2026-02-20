using UnityEngine;

namespace PrismPulse.Gameplay.Effects
{
    /// <summary>
    /// Creates particle effects at runtime for visual feedback.
    /// Bootstrap helper — replace with proper VFX Graph effects for final art.
    /// </summary>
    public static class ParticleEffectFactory
    {
        /// <summary>
        /// Small sparkle burst played at a target tile when it receives the correct beam color.
        /// </summary>
        public static ParticleSystem CreateTargetSatisfiedEffect(Vector3 position, Color color)
        {
            var go = new GameObject("TargetSparkle");
            go.transform.position = position + Vector3.back * 0.2f;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.4f;
            main.startSpeed = 2f;
            main.startSize = 0.08f;
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = color;
            main.gravityModifier = 0.3f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 16));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(1f, 0f)));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.3f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            // Use unlit particle material for bloom
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(color * 2f);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
            Object.Destroy(go, 1.5f);
            return ps;
        }

        /// <summary>
        /// Larger celebration burst when puzzle is solved.
        /// Spawns one burst per target color so all colors are clearly visible.
        /// </summary>
        public static void CreatePuzzleSolvedEffect(Vector3 position, Color[] levelColors)
        {
            if (levelColors == null || levelColors.Length == 0)
                levelColors = new[] { Color.white };

            int particlesPerColor = Mathf.Max(20, 60 / levelColors.Length);

            for (int i = 0; i < levelColors.Length; i++)
            {
                var color = levelColors[i];
                float delay = i * 0.08f;

                var go = new GameObject($"SolveCelebration_{i}");
                go.transform.position = position + Vector3.back * 0.3f;

                var ps = go.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = ps.main;
                main.playOnAwake = false;
                main.duration = 1f;
                main.loop = false;
                main.startLifetime = 1.2f;
                main.startSpeed = 3.5f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
                main.maxParticles = particlesPerColor;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.gravityModifier = 0.5f;
                main.startColor = color;

                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.burstCount = 1;
                emission.SetBurst(0, new ParticleSystem.Burst(delay, (short)particlesPerColor));

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.3f;

                var sizeOverLifetime = ps.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                    new AnimationCurve(
                        new Keyframe(0f, 0.5f),
                        new Keyframe(0.2f, 1f),
                        new Keyframe(1f, 0f)));

                var colorOverLifetimeModule = ps.colorOverLifetime;
                colorOverLifetimeModule.enabled = true;
                var fadeGradient = new Gradient();
                fadeGradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.3f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
                colorOverLifetimeModule.color = fadeGradient;

                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.material = CreateParticleMaterial(color * 2.5f);
                renderer.renderMode = ParticleSystemRenderMode.Billboard;

                ps.Play();
                Object.Destroy(go, 3f);
            }
        }

        private static Material CreateParticleMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            // Additive blending for glow
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 1f); // Additive
            mat.renderQueue = 3100;
            return mat;
        }
    }
}
