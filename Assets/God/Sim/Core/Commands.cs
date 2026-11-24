using System;

namespace God.Sim
{
    /// <summary>Base of every player and enemy action. Modules define subclasses (plain data).</summary>
    public abstract class Command
    {
        public int PlayerId;
    }

    /// <summary>Outcome of validating or applying a command.</summary>
    public readonly struct CommandResult
    {
        public readonly bool Ok;
        /// <summary>Reason code (see <see cref="Reasons"/>). Null when Ok.</summary>
        public readonly string Reason;
        /// <summary>Player-facing sentence. Null when Ok.</summary>
        public readonly string Message;

        public CommandResult(bool ok, string reason, string message)
        {
            Ok = ok;
            Reason = reason;
            Message = message;
        }

        public static CommandResult Accept() => new CommandResult(true, null, null);

        public static CommandResult Reject(string reason, string message) => new CommandResult(false, reason, message);

        /// <summary>Reject with the default sentence from <see cref="Reasons.Sentence"/> (args fill the template).</summary>
        public static CommandResult RejectDefault(string reason, params object[] args) =>
            new CommandResult(false, reason, Reasons.Sentence(reason, args));

        public override string ToString() => Ok ? "Ok" : Reason + ": " + Message;
    }

    /// <summary>
    /// Validates and applies one command type. Validate must not mutate state. Apply is called at the
    /// next tick boundary and must re-check anything that may have changed (it returns a result; a
    /// failure there is reported as a <see cref="CommandRejected"/> event and nothing is applied).
    /// </summary>
    public interface ICommandHandler
    {
        Type CommandType { get; }
        CommandResult Validate(GodState s, Command c);
        CommandResult Apply(GodState s, Command c, EventBuffer ev);
    }

    /// <summary>Typed convenience base for <see cref="ICommandHandler"/>.</summary>
    public abstract class CommandHandler<T> : ICommandHandler where T : Command
    {
        public Type CommandType => typeof(T);
        public CommandResult Validate(GodState s, Command c) => Validate(s, (T)c);
        public CommandResult Apply(GodState s, Command c, EventBuffer ev) => Apply(s, (T)c, ev);
        protected abstract CommandResult Validate(GodState s, T c);
        protected abstract CommandResult Apply(GodState s, T c, EventBuffer ev);
    }
}
