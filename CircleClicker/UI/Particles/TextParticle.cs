using System.Windows;
using System.Windows.Controls;

namespace CircleClicker.UI.Particles
{
    /// <summary>
    /// A particle containing text, which is affected by gravity and will last 1 second by default.
    /// </summary>
    public class TextParticle : Particle<TextBlock>
    {
        public TextParticle(Canvas canvas, string text, Point position = default)
            : base(canvas, new TextBlock() { Text = text }, position)
        {
            Random rng = new();
            Velocity = new Vector((rng.NextDouble() * 160) - 80, -200);
            MaxLifetime = 1;
        }

        public override void OnUpdate(double deltaTime)
        {
            Velocity.Y += deltaTime * 650;
            base.OnUpdate(deltaTime);

            if (Position.Y > Canvas.ActualHeight)
            {
                Destroy();
                return;
            }
            Element.Opacity = Math.Min(Lifetime * 10, 2 - Lifetime / MaxLifetime * 2);
        }
    }
}
