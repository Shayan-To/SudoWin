using System;
using System.Runtime.Serialization;

namespace Sudowin.Common
{
    [Serializable]
    public class SudoException : Exception
    {
        private SudoResultTypes _sudoResultType = SudoResultTypes.SudoError;    // default to generic error
        public SudoResultTypes SudoResultType
        {
            get { return _sudoResultType; }
        }

        public SudoException()
        {
        }

        public SudoException(SudoResultTypes sudoResultType, string message)
            : base(message)
        {
            _sudoResultType = sudoResultType;
        }

        public SudoException(SudoResultTypes sudoResultType, string message, Exception innerException)
            : base(message, innerException)
        {
            _sudoResultType = sudoResultType;
        }

        protected SudoException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _sudoResultType = (SudoResultTypes) info.GetValue("SudoResultType", typeof(SudoResultTypes));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("SudoResultType", SudoResultType);
        }

        public static SudoException GetException(SudoResultTypes sudoResultType)
        {
            return new SudoException(sudoResultType, getMessage(sudoResultType));
        }

        public static SudoException GetException(SudoResultTypes sudoResultType, Exception innerException)
        {
            return new SudoException(sudoResultType, getMessage(sudoResultType), innerException);
        }

        public static SudoException GetException(SudoResultTypes sudoResultType, params object[] args)
        {
            return new SudoException(sudoResultType, getMessage(sudoResultType, args));
        }

        public static SudoException GetException(SudoResultTypes sudoResultType, Exception innerException, params object[] args)
        {
            return new SudoException(sudoResultType, getMessage(sudoResultType, args), innerException);
        }

        private static string getMessage(SudoResultTypes sudoResultType, params object[] args)
        {
            string message = "";

            switch (sudoResultType)
            {
                case SudoResultTypes.InvalidLogon:
                    {
                        message = "Invalid logon attempt";
                        break;
                    }
                case SudoResultTypes.TooManyInvalidLogons:
                    {
                        message = "Invalid logon limit exceeded";
                        break;
                    }
                case SudoResultTypes.CommandNotAllowed:
                    {
                        message = "Command not allowed";
                        break;
                    }
                case SudoResultTypes.LockedOut:
                    {
                        message = "Locked out";
                        break;
                    }
                case SudoResultTypes.UsernameNotFound:
                    {
                        message = string.Format("Username {0}\\{1} not found", args[0], args[1]);
                        break;
                    }

                case SudoResultTypes.GroupNotFound:
                    {
                        message = string.Format("Group {0} not found", args[0]);
                        break;
                    }
                default:
                    {
                        if (args.Length == 0)
                        {
                            message = sudoResultType.ToString();
                        }
                        else if (args.Length == 1)
                        {
                            message = args[0].ToString();
                        }
                        else
                        {
                            // args[0] is format string
                            // args[1..n] are args (so need to shift left)

                            object[] argsNew = new object[args.Length - 1];
                            Array.Copy(args, 1, argsNew, 0, argsNew.Length);
                            message = string.Format(args[0].ToString(), argsNew);
                        }
                        break;
                    }

            }
            return message;

        }
    }
}
