namespace NorthwindTradersV7EnCapasConSignalIR
{
    public static class SesionActual
    {
        // Token JWT que devuelve el API al hacer login
        public static string Token { get; set; }

        // Usuario autenticado (opcional, si quieres guardar más datos)
        public static string Usuario { get; set; }
    }
}
