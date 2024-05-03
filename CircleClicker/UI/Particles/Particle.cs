using System.Windows;
using System.Windows.Controls;

namespace CircleClicker.UI.Particles
{
    public class Particle
    {
        /// <param name="particle">The particle that is being updated.</param>
        /// <param name="deltaTime">The time since the last frame, in seconds.</param>
        public delegate void UpdateEventHandler(Particle particle, double deltaTime);

        /// <param name="particle">The particle that is being destroyed.</param>
        public delegate void DestroyingEventHandler(Particle particle);

        /// <summary>
        /// Whether this particle has been destroyed.
        /// </summary>
        public bool Destroyed { get; private set; }

        /// <summary>
        /// The canvas this particle belongs to.
        /// </summary>
        public Canvas Canvas { get; init; }

        /// <summary>
        /// The <see cref="UIElement"/> that makes up the particle.
        /// </summary>
        public virtual UIElement Element { get; }

        /// <summary>
        /// The position of the particle on the canvas.
        /// </summary>
        public Point Position;

        /// <summary>
        /// How fast the particle is moving, in units per second.
        /// </summary>
        public Vector Velocity;

        /// <summary>
        /// Called every frame.
        /// </summary>
        public event UpdateEventHandler? Update;

        /// <summary>
        /// Called when the particle is being destroyed.
        /// </summary>
        public event DestroyingEventHandler? Destroying;

        /// <summary>
        /// The number of seconds this particle has been alive for.
        /// </summary>
        public double Lifetime;

        /// <summary>
        /// The number of seconds this particle will last before being destroyed.
        /// </summary>
        public double MaxLifetime = 60;

        public Particle(Canvas canvas, UIElement element, Point position = default)
        {
            Position = position;
            Element = element;
            Canvas = canvas;

            canvas.Children.Add(element);
        }

        /// <summary>
        /// Calls <see cref="Update"/>, updates the particle's <see cref="Position"/> and <see cref="Lifetime"/>, and destroys the particle if it reached its maximum lifetime.
        /// </summary>
        /// <param name="deltaTime">The time since the last frame, in seconds.</param>
        public virtual void OnUpdate(double deltaTime)
        {
            Lifetime += deltaTime;
            if (Lifetime > MaxLifetime)
            {
                Destroy();
                return;
            }

            Update?.Invoke(this, deltaTime);
            Position += Velocity * deltaTime;

            Canvas.SetLeft(Element, Position.X - Element.RenderSize.Width / 2);
            Canvas.SetTop(Element, Position.Y - Element.RenderSize.Height / 2);
        }

        /// <summary>
        /// Calls <see cref="Destroying"/>, removes the particle from the canvas and sets <see cref="Destroyed"/> to <see langword="true"/>.
        /// </summary>
        public void Destroy()
        {
            Destroying?.Invoke(this);
            Destroyed = true;
            Canvas.Children.Remove(Element);
        }

        /// <summary>
        /// Creates a new generic particle.
        /// </summary>
        public static Particle<T> Create<T>(Canvas canvas, T element, Point position = default)
            where T : UIElement
        {
            return new(canvas, element, position);
        }
    }

    public class Particle<T>(Canvas canvas, T element, Point position = default)
        : Particle(canvas, element, position)
        where T : UIElement
    {
        /// <param name="particle">The particle that is being updated.</param>
        /// <param name="deltaTime">The time since the last frame, in seconds.</param>
        public new delegate void UpdateEventHandler(Particle<T> particle, double deltaTime);

        public override T Element => (T)base.Element;

        /// <inheritdoc cref="Particle.Update"/>
        public new event UpdateEventHandler? Update
        {
            add => base.Update += value as Particle.UpdateEventHandler;
            remove => base.Update -= value as Particle.UpdateEventHandler;
        }
    }
}
