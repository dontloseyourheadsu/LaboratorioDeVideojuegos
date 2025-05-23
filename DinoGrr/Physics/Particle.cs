﻿namespace DinoGrr.Physics
{
    public class Particle
    {
        public Vector2 Position { get; set; }
        public Vector2 PreviousPosition { get; set; }
        public float Mass { get; set; }
        public bool IsInGround { get; set; }
        public bool Locked { get; set; }
        public bool IsCollisionActive { get; set; }
        public char BelongsTo { get; set; }

        public Particle(Vector2 position, float mass, char belongsTo)
        {
            Position = position;
            PreviousPosition = position;
            Mass = mass;
            IsCollisionActive = true;
            BelongsTo = belongsTo;
        }

        public void Update(int subStep, float gravity)
        {
            if (Locked)
            {
                return;
            }

            SetMaxVelocity(2.5f);

            Vector2 velocity = CalculateVelocity();
            Vector2 force = CalculateForce(subStep, gravity);
            Vector2 acceleration = force / Mass;
            Vector2 deltaTimeSquared = new Vector2(1,1) * (1.0f / subStep) * (1.0f / subStep);

            UpdatePositions(velocity, acceleration, deltaTimeSquared);
            IsInGround = false;
        }

        private void SetMaxVelocity(float maxVelocity)
        {
            if (Math.Abs(Position.X - PreviousPosition.X) > maxVelocity)
            {
                PreviousPosition.X += ((Position - PreviousPosition).Normalized() * maxVelocity).X;
            }
        }

        private Vector2 CalculateVelocity()
        {
            return new Vector2(2 * Position.X - PreviousPosition.X, 2 * Position.Y - PreviousPosition.Y);
        }

        private Vector2 CalculateForce(int subStep, float gravity)
        {
            Vector2 force = new Vector2(0.0f, gravity / subStep);
            if (IsInGround)
            {
                force = CalculateFrictionForce(subStep, gravity);
            }
            return force;
        }

        private Vector2 CalculateFrictionForce(int subStep, float gravity)
        {
            float frictionFactor = (gravity / subStep) * 1.5f;
            if (PreviousPosition.X < Position.X)
            {
                return new Vector2(-frictionFactor, gravity / subStep);
            }
            else if (PreviousPosition.X > Position.X)
            {
                return new Vector2(frictionFactor, gravity / subStep);
            }
            return new Vector2(0.0f, gravity / subStep);
        }

        private void UpdatePositions(Vector2 velocity, Vector2 acceleration, Vector2 deltaTimeSquared)
        {
            Vector2 prevPosition = new Vector2(Position.X, Position.Y);
            Position = new Vector2(velocity.X + acceleration.X * deltaTimeSquared.X,
                                   velocity.Y + acceleration.Y * deltaTimeSquared.Y);
            PreviousPosition = prevPosition;
        }

        public bool CheckPolygonCollision(Polygon polygon)
        {
            if (!IsCollisionActive)
            {
                return false;
            }

            var leftIntersectionCount = RayCasting.GetRayCastingCount(
                                    Position, polygon.sticks, rayLimit: 0);

            if (leftIntersectionCount % 2 != 0)
            {
                HandlePolygonCollision(polygon);
                return true;
            }

            return false;
        }

        private void HandlePolygonCollision(Polygon polygon)
        {
            var closestEdge = RayCasting.GetClosestEdge(Position, polygon.sticks);
            Stick stick = closestEdge.Item1;
            Vector2 closestPoint = closestEdge.Item2;

            var normalizedCollisionVector = (closestPoint - Position).Normalized();
            normalizedCollisionVector *= 8;

            if (!Locked)
            {
                PreviousPosition = Position;
                Position = closestPoint + normalizedCollisionVector;
            }

            if (stick is null || stick.A is null || stick.B is null) return;

            if (!stick.A.Locked)
            {
                stick.A.PreviousPosition = stick.A.Position;
                stick.A.Position -= normalizedCollisionVector;
            }

            if (stick.B.Locked) return;
            stick.B.PreviousPosition = stick.B.Position;
            stick.B.Position -= normalizedCollisionVector;
        }

        public void ParticleCollision(Particle otherParticle)
        {
            if (!IsCollisionActive)
            {
                return;
            }

            var distance = Position.Distance(otherParticle.Position);
            var sumRadius = Mass + otherParticle.Mass;

            if (distance < sumRadius)
            {
                var collisionVector = new Vector2(Position.X - otherParticle.Position.X,
                                                                      Position.Y - otherParticle.Position.Y);

                var collisionVectorNormalized = new Vector2(collisionVector.X / distance, collisionVector.Y / distance);

                var newPosition = new Vector2(Position.X + collisionVectorNormalized.X,
                                                                 Position.Y + collisionVectorNormalized.Y);

                Position = newPosition;
            }
        }

        public void KeepInsideCanvas(int width, int height)
        {
            if (Position.Y >= height-5)
            {
                Position.Y = height-5;
                IsInGround = true;
            }
            if (Position.X >= width-5)
            {
                Position.X = width-5;
            }
            if (Position.Y < 5)
            {
                Position.Y = 5;
            }
            if (Position.X < 5)
            { 
                Position.X = 5; 
            }
        }
    }
}
