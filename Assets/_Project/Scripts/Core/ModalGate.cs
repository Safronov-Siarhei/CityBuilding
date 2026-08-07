namespace CityBuilder.Core
{
    /// <summary>
    /// Shared switch that gameplay input systems (camera, building placement) check before
    /// reacting to input, so a modal dialog sitting visually on top of the 3D scene (which
    /// Update()-polled input has no built-in awareness of) actually blocks world interaction
    /// underneath it, not just its own UI raycasts.
    /// </summary>
    public static class ModalGate
    {
        public static bool IsBlocked { get; private set; }

        public static void SetBlocked(bool blocked)
        {
            IsBlocked = blocked;
        }

        /// <summary>Called once when the gameplay scene loads, so a dialog left open from a
        /// previous session (or a bug) can never permanently lock out input.</summary>
        public static void Reset()
        {
            IsBlocked = false;
        }
    }
}
