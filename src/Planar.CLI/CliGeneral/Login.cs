using Planar.CLI.Entities;

namespace Planar.CLI.CliGeneral
{
    internal static class Login
    {
        private static CliLoginRequest _login = new();

        public static CliLoginRequest Current => _login;

        public static void Set(CliLoginRequest? login)
        {
            if (login == null)
            {
                _login = new();
            }
            else
            {
                _login = login;
            }
        }
    }
}