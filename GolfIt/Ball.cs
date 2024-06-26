﻿using System.Drawing;

namespace GolfIt
{
    public class Ball : Verlet
    {
        public int cellSize;
        public Vector position;
        public Vector oldPosition;
        private Vector velocity;
        private Vector acceleration;
        private Vector pushVelocity;
        float friction = 0.99f;
        public float radius, diameter;
        Color color;
        public float mass = 1.0f;
        Brush brush;
        public bool isPinned;
        public List<Obstacle> obstacles;

        public Ball(int cellSize, Vector position, Vector velocity)
        {
            this.cellSize = cellSize;
            this.position = position;
            this.oldPosition = position;
            this.velocity = velocity;
            this.acceleration = new Vector(0, 0);
            this.pushVelocity = new Vector(0, 0);
            this.color = Color.White;
            this.brush = new SolidBrush(color);
            this.radius = cellSize / 2;
        }

        public void Update(Graphics g, PictureBox canvas, Map map)
        {
            if (!isPinned)
            {
                Vector newPosition;

                acceleration = pushVelocity / mass;
                pushVelocity = new Vector(0, 0);

                newPosition = position + (position - oldPosition) + acceleration;
                oldPosition = position;
                position = newPosition;

                velocity = position - oldPosition;

                bool sandCollision = CheckSandCollision(map);

                if (sandCollision)
                {
                    friction = 0.9f;
                }
                else
                {
                    friction = 0.99f;
                }

                velocity *= friction;
                position = oldPosition + velocity;

                if (position.X < 0 || position.X > canvas.Width)
                {
                    velocity.X = -velocity.X;
                }
                if (position.Y < 0 || position.Y > canvas.Height)
                {
                    velocity.Y = -velocity.Y;
                }

                Collision wallCollision = CheckWallCollision(map);

                if (wallCollision != Collision.None)
                {
                    if (wallCollision == Collision.TopLeft)
                    {
                        velocity.X = Math.Abs(velocity.X);
                        velocity.Y = Math.Abs(velocity.Y);
                    }
                    else if (wallCollision == Collision.TopRight)
                    {
                        velocity.X = -Math.Abs(velocity.X);
                        velocity.Y = Math.Abs(velocity.Y);
                    }
                    else if (wallCollision == Collision.BottomLeft)
                    {
                        velocity.X = Math.Abs(velocity.X);
                        velocity.Y = -Math.Abs(velocity.Y);
                    }
                    else if (wallCollision == Collision.BottomRight)
                    {
                        velocity.X = -Math.Abs(velocity.X);
                        velocity.Y = -Math.Abs(velocity.Y);
                    }
                    else if (wallCollision == Collision.Top)
                    {
                        velocity.Y = Math.Abs(velocity.Y);
                    }
                    else if (wallCollision == Collision.Bottom)
                    {
                        velocity.Y = -Math.Abs(velocity.Y);
                    }
                    else if (wallCollision == Collision.Left)
                    {
                        velocity.X = Math.Abs(velocity.X);
                    }
                    else if (wallCollision == Collision.Right)
                    {
                        velocity.X = -Math.Abs(velocity.X);
                    }
                }

                for (int i = 0; i < obstacles.Count; i++)
                {
                    var obstacleCollisionForce = obstacles[i].DetectCollision(this);

                    if (obstacleCollisionForce.Length() > 0 && obstacles[i] is Triangle)
                    {
                        velocity += obstacleCollisionForce / 4;
                    }

                    if (obstacles[i] is MovingFloor)
                    {
                        velocity += obstacleCollisionForce;
                    }
                }

                position = oldPosition + velocity;
            }

            Render(g);
        }

        public void Render(Graphics g)
        {
            int borderRadius = 2;
            g.FillEllipse(Brushes.Black, position.X - cellSize / 2 - borderRadius, position.Y - cellSize / 2 - borderRadius, cellSize + borderRadius * 2, cellSize + borderRadius * 2);
            g.FillEllipse(brush, position.X - cellSize / 2, position.Y - cellSize / 2, cellSize, cellSize);
            g.FillPie(Brushes.Gray, position.X - cellSize / 2, position.Y - cellSize / 2, cellSize, cellSize, 0, 90);
            g.FillEllipse(brush, position.X - cellSize / 2 + borderRadius / 2, position.Y - cellSize / 2 + borderRadius / 2, cellSize - borderRadius, cellSize - borderRadius);
        }

        public void PushBall(Vector pushVelocity)
        {
            this.pushVelocity = pushVelocity;
        }

        public bool IsMoving()
        {
            Vector currentVelocity = position - oldPosition;
            float speedThreshold = 0.01f;
            return currentVelocity.Length() > speedThreshold;
        }

        public bool CanEnterGoal()
        {
            Vector currentVelocity = position - oldPosition;
            float speedThreshold = .5f;
            return currentVelocity.Length() > speedThreshold;
        }

        public Collision CheckWallCollision(Map map)
        {
            float radius = cellSize / 2.0f - 3; 

            bool top = map.IsWall((int)(position.X), (int)(position.Y - radius));
            bool bottom = map.IsWall((int)(position.X), (int)(position.Y + radius));
            bool left = map.IsWall((int)(position.X - radius), (int)(position.Y));
            bool right = map.IsWall((int)(position.X + radius), (int)(position.Y));
            bool topLeft = map.IsWall((int)(position.X - radius), (int)(position.Y - radius));
            bool topRight = map.IsWall((int)(position.X + radius), (int)(position.Y - radius));
            bool bottomLeft = map.IsWall((int)(position.X - radius), (int)(position.Y + radius));
            bool bottomRight = map.IsWall((int)(position.X + radius), (int)(position.Y + radius));

            if (top) return Collision.Top;
            if (bottom) return Collision.Bottom;
            if (left) return Collision.Left;
            if (right) return Collision.Right;
            if (topLeft) return Collision.TopLeft;
            if (topRight) return Collision.TopRight;
            if (bottomLeft) return Collision.BottomLeft;
            if (bottomRight) return Collision.BottomRight;

            return Collision.None;
        }

        public bool CheckSandCollision(Map map)
        {
            float radius = cellSize / 2.0f + 2;

            bool top = map.IsSand((int)(position.X), (int)(position.Y - radius));
            bool bottom = map.IsSand((int)(position.X), (int)(position.Y + radius));
            bool left = map.IsSand((int)(position.X - radius), (int)(position.Y));
            bool right = map.IsSand((int)(position.X + radius), (int)(position.Y));
            bool topLeft = map.IsSand((int)(position.X - radius), (int)(position.Y - radius));
            bool topRight = map.IsSand((int)(position.X + radius), (int)(position.Y - radius));
            bool bottomLeft = map.IsSand((int)(position.X - radius), (int)(position.Y + radius));
            bool bottomRight = map.IsSand((int)(position.X + radius), (int)(position.Y + radius));

            return top || bottom || left || right || topLeft || topRight || bottomLeft || bottomRight;
        }
    }
}
