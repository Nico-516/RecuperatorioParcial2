using System;
using System.Collections.Generic;
using System.Linq;
using Parcial2.Models;
using Parcial2.Observer;
using Parcial2.Pagos;
using Parcial2.Repository;

namespace Parcial2.Controllers
{
    // PATRÓN OBSERVER — este controller es el Notificador (Publisher).
    // Hace su trabajo (validar, calcular, guardar) y AVISA. No sabe quién escucha.
    public class PedidoController : IPedidoPublisher
    {
        public const int MaximoObservaciones = 100;

        private readonly IRepository<Pedido> _pedidos;
        private readonly IRepository<Prenda> _prendas;
        private readonly List<IPedidoObserver> _suscriptores;

        // TODO 7 · Observer
        // Este controller tiene que ser el Notificador: guardar la lista de suscriptores,
        // permitir el alta y la baja, y avisarles a todos cuando algo pasa.

        public PedidoController(IRepository<Pedido> pedidos, IRepository<Prenda> prendas)
        {
            _pedidos = pedidos;
            _prendas = prendas;
        }

        public void Suscribir(IPedidoObserver observador)
        {
            if (_suscriptores.Contains(observador)) { throw new ArgumentException("El observador ya esta suscripto."); }
            _suscriptores.Add(observador);
            return;
        }

        public void Desuscribir(IPedidoObserver observador)
        {
            if (_suscriptores.Contains(observador)) { _suscriptores.Remove(observador); return; }
            throw new ArgumentException("El observador no esta suscripto");
        }

        private void Notificar(EventoPedido evento)
        {
            foreach (IPedidoObserver suscriptor in _suscriptores.ToList())
            {
                suscriptor.Actualizar(evento);
            }
        }

        // ------------------------------------------------------------------ CONSULTAS

        public List<Prenda> ObtenerCatalogo()
        {
            return _prendas.LeerTodos().OrderBy(p => p.Categoria).ThenBy(p => p.Nombre).ToList();
        }

        public List<Pedido> ObtenerPedidos()
        {
            return _pedidos.LeerTodos().OrderByDescending(p => p.Id).ToList();
        }

        // Lo que tiene que mirar la persona de los lavarropas: lo que espera y lo que se está lavando.
        public List<Pedido> PedidosEnProceso()
        {
            return _pedidos.LeerTodos()
                .Where(p => p.Estado == EstadoPedido.EnEspera || p.Estado == EstadoPedido.Lavando)
                .OrderBy(p => p.Id)
                .ToList();
        }

        public int Contar(EstadoPedido estado)
        {
            return _pedidos.LeerTodos().Count(p => p.Estado == estado);
        }

        public decimal Facturacion()
        {
            return _pedidos.LeerTodos().Where(p => p.Estado != EstadoPedido.Cancelado).Sum(p => p.Total);
        }

        public List<string> FormasDePago()
        {
            return PagoFactory.TiposDisponibles();
        }

        // La vista lo usa para habilitar "Cuotas" y "CUIT" según la forma de pago elegida.
        public IPagoStrategy InfoPago(string tipo)
        {
            return PagoFactory.Crear(tipo);
        }

        // ------------------------------------------------------------------ ARMADO DEL PEDIDO

        public Pedido NuevoBorrador()
        {
            return new Pedido();
        }

        public void AgregarItem(Pedido borrador, int idPrenda, int cantidad)
        {
            Prenda prenda = _prendas.BuscarPorId(idPrenda);
            borrador.AgregarItem(prenda, cantidad);
        }

        // TODO 6 · Controller
        // CotizarPago: usá la factory para obtener la estrategia y delegale el cálculo (redondeado a 0 decimales).
        // ConfirmarPedido: validar, completar el borrador, calcular ajuste y total, guardar
        //                  y notificar el alta (TODO 7). Reglas en el README.

        public decimal CotizarPago(string tipoPago, decimal subtotal, int cuotas)
        {
            var estrategia = PagoFactory.Crear(tipoPago);
            return Math.Round(estrategia.CalcularAjuste(subtotal, cuotas), 0);
        }

        public Pedido ConfirmarPedido(Pedido borrador, string cliente, string telefono,
                                      string observaciones, string cuit, int cuotas, string tipoPago)
        {
            if (string.IsNullOrWhiteSpace(cliente)) { throw new ArgumentException("El cliente es obligatorio."); }
            if (borrador == null || borrador.Items.Count() <= 0) { throw new ArgumentException("El pedido no puede estar vacio."); }
            string telefonoFiltrado = telefono.Trim('-').Trim();
            if (telefonoFiltrado.Count() < 10 || telefonoFiltrado.Count() > 10) { throw new ArgumentException("El telefono debe tener 10 digitos."); }
            if (observaciones.Count() > 100) { throw new ArgumentException("Las observaciones no pueden superar los 100 caracteres."); }
            var estrategia = PagoFactory.Crear(tipoPago);
            if (estrategia.RequiereCuit == true && string.IsNullOrWhiteSpace(cuit)) { throw new ArgumentException($"El tipo de pago {tipoPago}, requiere de un cuit valido."); }
            if (estrategia.PermiteCuotas == false) { cuotas = 1; }
            if (estrategia.RequiereCuit == false) { cuit = ""; }
            decimal subtotal = borrador.CalcularSubtotal();
            decimal ajuste = estrategia.CalcularAjuste(subtotal, cuotas);
            decimal total = subtotal + ajuste;
            borrador.Estado = EstadoPedido.EnEspera;
            var fecha = DateTime.Now;

            _pedidos.Agregar(borrador);
            return borrador;
        }

        // ------------------------------------------------------------------ OPERACIONES

        public void AvanzarEstado(int id)
        {
            Pedido pedido = BuscarOFallar(id);
            EstadoPedido anterior = pedido.Estado;
            pedido.AvanzarEstado();
            _pedidos.Actualizar(pedido);

            Notificar(new EventoPedido(TipoEvento.CambioEstado, pedido, anterior));
        }

        public void Cancelar(int id)
        {
            Pedido pedido = BuscarOFallar(id);
            EstadoPedido anterior = pedido.Estado;
            pedido.Cancelar();
            _pedidos.Actualizar(pedido);

            Notificar(new EventoPedido(TipoEvento.Cancelacion, pedido, anterior));
        }

        private Pedido BuscarOFallar(int id)
        {
            return _pedidos.BuscarPorId(id) ?? throw new InvalidOperationException($"No existe el pedido #{id}.");
        }
    }
}
