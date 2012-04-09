using System;
using System.Windows.Interactivity;
using System.Windows;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace Usoniandream.WPF.Extensions
{
    public sealed class MouseIdleBehavior : Behavior<Window>
    {
        private IDisposable mouseObserver = null;
        private DispatcherTimer timer = null;
        private Point previousPosition;
        public bool IsHidden { get; set; }

        /// <summary>
        /// Dependency property for the <see cref="P:TimeToIdle"/> property.
        /// </summary>
        public static readonly DependencyProperty TimeToIdleProperty = DependencyProperty.Register("TimeToIdle", typeof(int), typeof(MouseIdleBehavior), new PropertyMetadata(default(int)));

        /// <summary>
        /// Milliseconds to wait before comparing mouse locations.
        /// </summary>
        public int TimeToIdle
        {
            get { return (int)GetValue(TimeToIdleProperty); }
            set { SetValue(TimeToIdleProperty, value); }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (previousPosition == Mouse.GetPosition(AssociatedObject))
            {
                IsHidden = true;
                Mouse.OverrideCursor = Cursors.None;
            }
        }

        /// <summary>
        /// Stores mouse position and, if hidden, brings back the mouse pointer when movement starts again. 
        /// </summary>
        /// <param name="point"></param>
        private void HandleMouseMovement(Point point)
        {
            if (IsHidden)
            {
                IsHidden = false;
                Mouse.OverrideCursor = Cursors.Arrow;
            }
            previousPosition = point;
        }

        protected override void OnAttached()
        {
            /// wire timer and set the tick interval to specified milliseconds, or default to 3000 if not set.
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(TimeToIdle == 0 ? 3000 : TimeToIdle);
            timer.Tick += new EventHandler(timer_Tick);

            // using Rx (reactive extensions) to get mouse movement..
            var mouseMove = from evt in Observable.FromEventPattern<MouseEventArgs>(AssociatedObject, "MouseMove").ObserveOnDispatcher()
                            select evt.EventArgs.GetPosition(AssociatedObject);

            // point to the IDisposable variable so we can clean it up later, then subscribe and continue..
            mouseObserver = mouseMove.ObserveOnDispatcher()
                                     .Subscribe
                                        (
                                            x => HandleMouseMovement(x)                        
                                        );

            timer.Start();

            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            // gracefully clean up the Rx observable..
            if (mouseObserver!=null)
            {
                mouseObserver.Dispose();
            }

            // .. and get rid of the timer.
            timer.Tick -= timer_Tick;
            timer.Stop();
            timer = null;

            base.OnDetaching();
        }

    }
}
