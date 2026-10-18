using SmartX.API.Models;

namespace SmartX.API.Services
{
    /// <summary>
    /// Keeps every manual override on a Stack&lt;T&gt; (last in, first out), so the
    /// operator can always undo the most recent command first.
    /// A small dictionary tracks each control's current state so a command
    /// knows what to revert to.
    /// </summary>
    public class CommandHistory
    {
        private readonly Stack<Command> _history = new();
        private readonly Dictionary<string, string> _states = new();
        private readonly object _lock = new();

        private static string Key(string deviceId, string name) => $"{deviceId}|{name}";

        /// <summary>Applies a command and pushes it onto the history stack.</summary>
        public Command Issue(IssueCommandRequest request)
        {
            lock (_lock)
            {
                var key = Key(request.DeviceId, request.Name);
                var previous = _states.GetValueOrDefault(key, "Off");

                var command = new Command
                {
                    DeviceId = request.DeviceId,
                    Name = request.Name,
                    State = request.State,
                    PreviousState = previous
                };

                _states[key] = command.State;
                _history.Push(command);
                return command;
            }
        }

        /// <summary>Pops the latest command and restores the control's earlier state.</summary>
        public Command? Undo()
        {
            lock (_lock)
            {
                if (!_history.TryPop(out var last)) return null;

                _states[Key(last.DeviceId, last.Name)] = last.PreviousState;
                return last;
            }
        }

        /// <summary>Newest command first (a Stack enumerates in last-in, first-out order).</summary>
        public List<Command> History()
        {
            lock (_lock) return _history.ToList();
        }
    }
}