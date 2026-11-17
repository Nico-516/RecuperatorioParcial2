using System;
using System.Collections.Generic;
using System.Linq;

namespace Parcial2.Models
{
    public class Pedido : IEntidad
    {
        public const int MaximoPorItem = 20;

        public int Id { get; set; }
        public string Cliente { get; set; } = "";
        public string Telefono { get; set; } = "";       // se guarda solo con dígitos: 2235550101
        public string Observaciones { get; set; } = "";  // opcional: "sin suavizante", "mancha en la manga"
        public string Cuit { get; set; } = "";

        // Nombre de la forma de pago: "Efectivo", "Tarjeta", "Cuenta empresa".
        // En el JSON se guarda como TEXTO.
        public string FormaPago { get; set; } = "";
        public int Cuotas { get; set; } = 1;

        public List<ItemPedido> Items { get; set; } = new List<ItemPedido>();

        // Se congelan al confirmar el pedido. Ajuste negativo = descuento; positivo = recargo.
        public decimal Ajuste { get; set; }
        public decimal Total { get; set; }

        public EstadoPedido Estado { get; set; } = EstadoPedido.EnEspera;
        public DateTime FechaIngreso { get; set; } = DateTime.Now;

        // TODO 2 · Modelo y validaciones. Reglas completas en el README. Resumen:
        //  - AgregarItem: solo si el pedido está EnEspera (si no, InvalidOperationException);
        //    prenda obligatoria; cantidad entre 1 y MaximoPorItem; si la prenda ya está, se SUMA la cantidad.
        //  - CalcularSubtotal: suma de los subtotales de los ítems.
        //  - AvanzarEstado: EnEspera -> Lavando -> ParaRetirar -> Entregado. Un pedido finalizado no avanza.
        //  - Cancelar: solo si está EnEspera o Lavando.
        // Dato inválido: ArgumentException. Operación que no corresponde: InvalidOperationException.

        public void AgregarItem(Prenda prenda, int cantidad)
        {
            if (Estado != EstadoPedido.EnEspera)
            {
                throw new InvalidOperationException("El pedido debe estar en espera.");
            }

            if (prenda == null){ throw new ArgumentException("Se debe ingresar una prenda."); }
            if (cantidad < 1 || cantidad > 20) { throw new ArgumentException("Solo se permite la catidad entre 1 y 20."); }

            ItemPedido existente = Items.FirstOrDefault(i => i.Descripcion == prenda.Nombre);
            if (existente != null)
            {
                if (existente.Cantidad + cantidad > MaximoPorItem)
                    throw new ArgumentException($"No se pueden pedir más de {MaximoPorItem} unidades de {prenda.Nombre}.");

                existente.Cantidad += cantidad;
                return;
            }
        }

        public decimal CalcularSubtotal()
        {
            return Items.Sum(i => i.Subtotal);
        }

        public bool EstaFinalizado()
        {
            return Estado == EstadoPedido.Entregado || Estado == EstadoPedido.Cancelado;
        }

        public void AvanzarEstado()
        {
            if (Estado == EstadoPedido.EnEspera) { Estado = EstadoPedido.Lavando; return; }
            if (Estado == EstadoPedido.Lavando) { Estado = EstadoPedido.ParaRetirar; return; }
            if (Estado == EstadoPedido.ParaRetirar) { Estado = EstadoPedido.Entregado; return; }
            throw new ArgumentException("No se puede avanzar el pedido si ya esta entregado o cancelado.");
        }

        public void Cancelar()
        {
            if (Estado == EstadoPedido.EnEspera || Estado == EstadoPedido.Lavando) { Estado = EstadoPedido.Cancelado; return; }
            throw new ArgumentException("Solo se puede cancelar cuando el pedido este ´En espera´ o ´Lavando´.");
        }
    }
}
