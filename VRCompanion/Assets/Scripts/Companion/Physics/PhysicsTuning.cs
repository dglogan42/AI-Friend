using UnityEngine;

namespace VRCompanion.PhysicsTuning
{
    /// <summary>
    /// Central place for companion-world physics defaults and prop helpers.
    /// Values chosen for stable VR tabletop props (not heavy simulation).
    /// </summary>
    public static class PhysicsTuning
    {
        public const float DefaultGravityY = -9.81f;
        public const float SoftBounce = 0.25f;
        public const float TabletopFriction = 0.55f;
        public const float PropMassKg = 0.35f;
        public const float PropDrag = 0.4f;
        public const float PropAngularDrag = 0.6f;
        public const float SleepThreshold = 0.005f;

        /// <summary>Apply project-wide defaults (safe to call multiple times).</summary>
        public static void ApplyProjectDefaults()
        {
            Physics.gravity = new Vector3(0f, DefaultGravityY, 0f);
            Physics.defaultContactOffset = 0.01f;
            Physics.bounceThreshold = 0.5f;
            Physics.sleepThreshold = SleepThreshold;
            Physics.defaultSolverIterations = 8;
            Physics.defaultSolverVelocityIterations = 2;
        }

        public static PhysicsMaterial CreatePropMaterial(float bounciness = SoftBounce, float friction = TabletopFriction)
        {
            var mat = new PhysicsMaterial("CompanionProp")
            {
                bounciness = Mathf.Clamp01(bounciness),
                dynamicFriction = Mathf.Clamp01(friction),
                staticFriction = Mathf.Clamp01(friction + 0.05f),
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Multiply
            };
            return mat;
        }

        /// <summary>
        /// Adds a kinematic static collider setup for floors, or dynamic rigidbody for loose props.
        /// </summary>
        public static void ConfigureCollider(GameObject go, bool isStatic, float mass = PropMassKg)
        {
            if (go == null)
                return;

            var col = go.GetComponent<Collider>();
            if (col == null)
                col = go.AddComponent<BoxCollider>();

            col.sharedMaterial = CreatePropMaterial();

            if (isStatic)
            {
                var rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                    Object.Destroy(rb);
                return;
            }

            var body = go.GetComponent<Rigidbody>();
            if (body == null)
                body = go.AddComponent<Rigidbody>();
            body.mass = Mathf.Max(0.01f, mass);
            body.linearDamping = PropDrag;
            body.angularDamping = PropAngularDrag;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        /// <summary>
        /// Analytic bounce height under constant gravity after an upward impulse (for tests).
        /// h = v^2 / (2g)
        /// </summary>
        public static float BounceHeightMeters(float upwardSpeed, float gravityY = DefaultGravityY)
        {
            float g = Mathf.Abs(gravityY);
            if (g < 1e-6f || upwardSpeed <= 0f)
                return 0f;
            return (upwardSpeed * upwardSpeed) / (2f * g);
        }

        /// <summary>Time to apex: t = v / g</summary>
        public static float TimeToApexSeconds(float upwardSpeed, float gravityY = DefaultGravityY)
        {
            float g = Mathf.Abs(gravityY);
            if (g < 1e-6f || upwardSpeed <= 0f)
                return 0f;
            return upwardSpeed / g;
        }
    }
}
