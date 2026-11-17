using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parcial2.Pagos
{
    class PagoCuentaEmpresa : IPagoStrategy
    {
        public string Nombre => "Cuenta Empresa";
        public bool PermiteCuotas => false;
        public bool RequiereCuit => true;

        public decimal CalcularAjuste(decimal subtotal, int cuotas)
        {
            if (subtotal >= 20000 || subtotal <= 100000) { return -subtotal * 0.05m; }
            if (subtotal > 100000) { return -subtotal * 0.08m; }
            throw new InvalidOperationException("La cuenta empresa admite pedidos desde $20.000.");
        }
    }
}
