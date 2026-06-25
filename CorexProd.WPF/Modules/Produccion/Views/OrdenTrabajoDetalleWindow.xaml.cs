using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class OrdenTrabajoDetalleWindow : Window
    {
        private readonly int _id;
        private readonly OrdenTrabajoNegocio _negocio = new();
        private OrdenTrabajo _ot = null!;
        private List<OrdenTrabajoDetalleArea> _visibles = [];
        private readonly bool _puedeOperarOt;

        public OrdenTrabajoDetalleWindow(int id)
        {
            InitializeComponent();
            _id = id;
            _puedeOperarOt = PermissionService.PuedeOperarOrdenTrabajo;
            try
            {
                Cargar();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo abrir la OT: {ex.Message}");
                Loaded += (_, _) => Close();
            }
        }

        private void Cargar()
        {
            _ot = _negocio.Obtener(_id) ?? throw new InvalidOperationException("No se encontró la OT.");
            NumeroText.Text = $"Número OT: {_ot.NumeroOT}";
            OciText.Text = $"Número OCI: {_ot.NumeroOci}";
            OrdenCompraText.Text = $"Orden de compra: {_ot.OrdenCompraCliente}";
            ClienteText.Text = $"Cliente: {_ot.NombreCliente}";
            EstadoText.Text = $"Estado: {_ot.Estado}";
            CargarResumen();

            List<OrdenTrabajoDetalleArea> areas = _ot.Areas.GroupBy(x => x.IdAreaProduccion).Select(g => g.First()).OrderBy(x => x.OrdenSecuencia).ToList();
            int? seleccionada = (AreaCombo.SelectedItem as OrdenTrabajoDetalleArea)?.IdAreaProduccion;
            AreaCombo.ItemsSource = areas;
            AreaCombo.SelectedItem = areas.FirstOrDefault(x => x.IdAreaProduccion == seleccionada) ?? areas.FirstOrDefault();
            MostrarArea();
        }

        private void CargarResumen()
        {
            ResumenGrid.Columns.Clear();
            DataTable tabla = new();
            tabla.Columns.Add("Codigo", typeof(string));
            tabla.Columns.Add("Producto", typeof(string));
            tabla.Columns.Add("Cantidad", typeof(decimal));

            Style productoStyle = new(typeof(TextBlock));
            productoStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            productoStyle.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.None));
            productoStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            productoStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(4, 6, 4, 6)));

            ResumenGrid.Columns.Add(new DataGridTextColumn { Header = "Codigo", Binding = new Binding("Codigo"), Width = 115 });
            ResumenGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Nombre de producto",
                Binding = new Binding("Producto"),
                Width = 270,
                ElementStyle = productoStyle
            });
            ResumenGrid.Columns.Add(new DataGridTextColumn { Header = "Cant.\nRequerida", Binding = new Binding("Cantidad") { StringFormat = "N2" }, Width = 90 });

            List<OrdenTrabajoDetalleArea> areas = _ot.Areas.GroupBy(x => x.IdAreaProduccion).Select(g => g.First()).OrderBy(x => x.OrdenSecuencia).ToList();
            for (int i = 0; i < areas.Count; i++)
            {
                string propiedad = $"A{areas[i].IdAreaProduccion}";
                tabla.Columns.Add(propiedad, typeof(ResumenAreaCelda));
                ResumenGrid.Columns.Add(CrearColumnaArea(areas[i], propiedad, i));
            }

            tabla.Columns.Add("Terminado", typeof(decimal));
            tabla.Columns.Add("Estado", typeof(string));
            tabla.Columns.Add("Pendientes", typeof(string));
            ResumenGrid.Columns.Add(new DataGridTextColumn { Header = "Terminado", Binding = new Binding("Terminado") { StringFormat = "N2" }, Width = 100 });
            ResumenGrid.Columns.Add(new DataGridTextColumn { Header = "Estado producción", Binding = new Binding("Estado"), Width = 130 });
            Style pendientesStyle = new(typeof(TextBlock));
            pendientesStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            pendientesStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            pendientesStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(180, 83, 9))));
            pendientesStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
            ResumenGrid.Columns.Add(new DataGridTextColumn { Header = "Pendientes", Binding = new Binding("Pendientes"), Width = 145, ElementStyle = pendientesStyle });

            foreach (OrdenTrabajoDetalle detalle in _ot.Detalles)
            {
                DataRow fila = tabla.NewRow();
                fila["Codigo"] = detalle.CodigoProducto;
                fila["Producto"] = detalle.NombreProducto;
                fila["Cantidad"] = detalle.CantidadRequerida;
                for (int i = 0; i < areas.Count; i++)
                {
                    OrdenTrabajoDetalleArea? area = _ot.Areas.FirstOrDefault(x => x.IdDetalleOT == detalle.IdDetalleOT && x.IdAreaProduccion == areas[i].IdAreaProduccion);
                    if (area == null)
                    {
                        fila[$"A{areas[i].IdAreaProduccion}"] = DBNull.Value;
                        continue;
                    }
                    string destino = i + 1 < areas.Count ? areas[i + 1].NombreArea : string.Empty;
                    fila[$"A{areas[i].IdAreaProduccion}"] = new ResumenAreaCelda(area, destino, i);
                }
                fila["Terminado"] = detalle.CantidadProducida;
                fila["Estado"] = detalle.Estado;
                decimal pendienteNuevaOt = Math.Max(0, detalle.CantidadRequerida - detalle.CantidadProducida);
                fila["Pendientes"] = detalle.Estado == "TERMINADO"
                    ? pendienteNuevaOt > 0 ? $"{pendienteNuevaOt:0.##} por producir\nNueva OT desde OCI" : "Completo"
                    : string.Empty;
                tabla.Rows.Add(fila);
            }
            ResumenGrid.ItemsSource = tabla.DefaultView;
        }

        private DataGridTemplateColumn CrearColumnaArea(OrdenTrabajoDetalleArea area, string propiedad, int indice)
        {
            FrameworkElementFactory boton = new(typeof(Button));
            boton.SetBinding(ContentControl.ContentProperty, new Binding(propiedad));
            boton.SetBinding(FrameworkElement.TagProperty, new Binding(propiedad));
            boton.SetBinding(FrameworkElement.ToolTipProperty, new Binding($"{propiedad}.Ayuda"));
            boton.SetValue(Control.PaddingProperty, new Thickness(5, 4, 5, 4));
            boton.SetValue(Control.MarginProperty, new Thickness(3));
            boton.SetValue(FrameworkElement.HeightProperty, 60.0);
            boton.SetValue(Control.FontWeightProperty, FontWeights.SemiBold);
            boton.SetValue(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center);
            boton.SetValue(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center);
            boton.SetBinding(Control.BackgroundProperty, new Binding($"{propiedad}.Fondo"));
            boton.SetBinding(Control.ForegroundProperty, new Binding($"{propiedad}.TextoColor"));
            boton.SetValue(UIElement.IsEnabledProperty, _puedeOperarOt);

            FrameworkElementFactory contenido = new(typeof(StackPanel));
            contenido.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
            FrameworkElementFactory primeraFila = new(typeof(StackPanel));
            primeraFila.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            primeraFila.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            FrameworkElementFactory pendiente = new(typeof(TextBlock));
            pendiente.SetValue(TextBlock.FontSizeProperty, 11.0);
            pendiente.SetBinding(TextBlock.TextProperty, new Binding("Area.CantidadPendiente") { StringFormat = "P: {0:N2}" });
            FrameworkElementFactory enviado = new(typeof(TextBlock));
            enviado.SetValue(TextBlock.FontSizeProperty, 11.0);
            enviado.SetValue(FrameworkElement.MarginProperty, new Thickness(7, 0, 0, 0));
            enviado.SetBinding(TextBlock.TextProperty, new Binding("Area.CantidadEnviada") { StringFormat = "E: {0:N2}" });
            primeraFila.AppendChild(pendiente);
            primeraFila.AppendChild(enviado);
            FrameworkElementFactory merma = new(typeof(TextBlock));
            merma.SetValue(TextBlock.FontSizeProperty, 11.0);
            merma.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            merma.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 3, 0, 0));
            merma.SetBinding(TextBlock.TextProperty, new Binding("Area.CantidadMerma") { StringFormat = "M: {0:N2}" });
            contenido.AppendChild(primeraFila);
            contenido.AppendChild(merma);
            boton.SetValue(ContentControl.ContentTemplateProperty, new DataTemplate { VisualTree = contenido });
            boton.AddHandler(Button.ClickEvent, new RoutedEventHandler(AreaResumen_Click));
            return new DataGridTemplateColumn { Header = area.NombreArea, Width = 125, CellTemplate = new DataTemplate { VisualTree = boton } };
        }

        private void AreaResumen_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermisoOperacion()) return;

            if ((sender as FrameworkElement)?.Tag is not ResumenAreaCelda celda) return;
            if (!celda.Area.EsTermino && string.IsNullOrWhiteSpace(celda.AreaDestino))
            {
                MessageBox.Show(this, $"{celda.Area.NombreArea} es el área de término y no tiene un destino siguiente.", "Área final", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (celda.Area.CantidadPendiente <= 0)
            {
                MessageBox.Show(this, $"No hay stock en {celda.Area.NombreArea} disponible para transferir.", "Sin stock", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool esTerminacion = celda.Area.EsTermino;
            OrdenTrabajoDetalle? detalle = _ot.Detalles.FirstOrDefault(x => x.IdDetalleOT == celda.Area.IdDetalleOT);
            bool permiteAjusteInicial = celda.EsPrimerProceso && detalle?.Estado == "PENDIENTE";
            TransferirProductoWindow ventana = new(
                celda.Area,
                esTerminacion ? "Productos terminados" : celda.AreaDestino,
                esTerminacion,
                permiteAjusteInicial)
            {
                Owner = this
            };
            if (ventana.ShowDialog() != true) return;
            try
            {
                int completadosAntes = _ot.Detalles.Count(x => x.Estado == "TERMINADO");
                string estadoAntes = _ot.Estado;
                string nombreUsuario = SessionManager.UsuarioActual?.NombreUsuario ?? string.Empty;
                Usuario autoriza = _negocio.Autorizar(nombreUsuario, ventana.Clave);
                OrdenTrabajoTransferenciaItem item = new() { IdDetalleOT = celda.Area.IdDetalleOT, Cantidad = ventana.Cantidad };
                int idSesion = SessionManager.UsuarioActual?.IdUsuario ?? 0;
                if (permiteAjusteInicial)
                {
                    decimal cantidadLanzada = ventana.Cantidad + (ventana.RegistrarMerma ? ventana.CantidadMerma : 0);
                    _negocio.Lanzar(
                        _id,
                        idSesion,
                        autoriza,
                        [
                            new OrdenTrabajoLanzamiento
                            {
                                IdDetalleOT = celda.Area.IdDetalleOT,
                                CantidadLanzada = cantidadLanzada,
                                Motivo = detalle != null && cantidadLanzada != detalle.CantidadPlanificada ? "AJUSTE POR APROVECHAMIENTO" : string.Empty,
                                Observacion = cantidadLanzada > celda.Area.CantidadPendiente
                                    ? $"Cantidad ajustada en area de inicio de {celda.Area.CantidadPendiente:N2} a {cantidadLanzada:N2}."
                                    : string.Empty
                            }
                        ]);
                }
                long operacion = ventana.RegistrarMerma
                    ? esTerminacion
                        ? _negocio.TerminarConMerma(_id, celda.Area.IdAreaProduccion, celda.Area.IdDetalleArea, idSesion, autoriza, ventana.CantidadMerma, ventana.MotivoMerma, ventana.ObservacionMerma, [item])
                        : _negocio.TransferirConMerma(_id, celda.Area.IdAreaProduccion, celda.Area.IdDetalleArea, idSesion, autoriza, ventana.CantidadMerma, ventana.MotivoMerma, ventana.ObservacionMerma, [item])
                        : esTerminacion
                        ? _negocio.Terminar(_id, celda.Area.IdAreaProduccion, idSesion, autoriza, string.Empty, [item])
                        : _negocio.Transferir(_id, celda.Area.IdAreaProduccion, idSesion, autoriza, string.Empty, [item]);
                Cargar();
                int totalProductos = _ot.Detalles.Count;
                int completadosDespues = _ot.Detalles.Count(x => x.Estado == "TERMINADO");
                decimal saldoOrigen = Math.Max(0, celda.Area.CantidadPendiente - ventana.Cantidad - (ventana.RegistrarMerma ? ventana.CantidadMerma : 0));
                string titulo = esTerminacion ? "Producto Terminado Correctamente" : "Transferencia Realizada Correctamente";
                string mensaje = esTerminacion
                    ? $"✅ Producto {completadosDespues} de {totalProductos} completado correctamente."
                    : $"✅ Proceso completado con éxito.\n\nSe transfirieron {ventana.Cantidad:N3} unidades desde el área {celda.Area.NombreArea} hacia {celda.AreaDestino}.";
                string estado = saldoOrigen <= 0
                    ? "🎉 No existen unidades pendientes en el área de origen. El flujo de producción continúa correctamente."
                    : "El área de origen aún mantiene saldo pendiente.";
                new ProduccionResultadoWindow(
                    titulo,
                    mensaje,
                    celda.Area.NombreArea,
                    esTerminacion ? "PRODUCTOS TERMINADOS" : celda.AreaDestino,
                    ventana.Cantidad,
                    saldoOrigen,
                    estado)
                {
                    Owner = this
                }.ShowDialog();

                if (estadoAntes != "TERMINADA" && _ot.Estado == "TERMINADA")
                {
                    new ProduccionResultadoWindow(
                        "Producción finalizada exitosamente",
                        "🎉 Todos los productos de la Orden de Trabajo fueron procesados.\n\nGenerando movimientos de inventario...",
                        "PRODUCCION",
                        "INVENTARIO",
                        _ot.Detalles.Sum(x => x.CantidadProducida),
                        0,
                        "Kardex generado correctamente.")
                    {
                        Owner = this
                    }.ShowDialog();
                    new OrdenTrabajoKardexWindow(_ot) { Owner = this }.ShowDialog();
                }
            }
            catch (Exception ex) { NotificationService.Error(ex.Message); }
        }

        private void MostrarArea()
        {
            if (AreaCombo.SelectedItem is not OrdenTrabajoDetalleArea area) return;
            _visibles = _ot.Areas.Where(x => x.IdAreaProduccion == area.IdAreaProduccion).ToList();
            ItemsGrid.ItemsSource = _visibles;
        }

        private void AreaCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => MostrarArea();
        private void EnviarTodos_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermisoOperacion()) return;

            foreach (OrdenTrabajoDetalleArea x in _visibles)
            {
                x.Seleccionado = x.Disponible;
                x.CantidadOperacion = x.Disponible ? x.CantidadPendiente : 0;
            }

            ItemsGrid.Items.Refresh();
        }

        private void Transferir_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermisoOperacion()) return;

            try
            {
                ItemsGrid.CommitEdit();
                if (AreaCombo.SelectedItem is not OrdenTrabajoDetalleArea area) return;
                List<OrdenTrabajoTransferenciaItem> items = _visibles.Where(x => x.Seleccionado).Select(x => new OrdenTrabajoTransferenciaItem { IdDetalleOT = x.IdDetalleOT, Cantidad = x.CantidadOperacion }).ToList();
                AutorizacionOperacionWindow auth = new("Autorizar transferencia grupal") { Owner = this };
                if (auth.ShowDialog() != true) return;
                Usuario usuario = _negocio.Autorizar(auth.Usuario, auth.Clave);
                long op = _negocio.Transferir(_id, area.IdAreaProduccion, SessionManager.UsuarioActual?.IdUsuario ?? 0, usuario, auth.Observacion, items);
                NotificationService.Success($"Transferencia grupal #{op} confirmada para {items.Count} producto(s).");
                Cargar();
            }
            catch (Exception ex) { NotificationService.Error(ex.Message); }
        }

        private void Iniciar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermisoOperacion()) return;

            try
            {
                ItemsGrid.CommitEdit();
                Dictionary<int, OrdenTrabajoDetalleArea> seleccion = _visibles.Where(x => x.Seleccionado).ToDictionary(x => x.IdDetalleOT);
                List<OrdenTrabajoDetalle> pendientes = _ot.Detalles.Where(x => x.Estado == "PENDIENTE" && seleccion.ContainsKey(x.IdDetalleOT)).ToList();
                if (pendientes.Count == 0) throw new InvalidOperationException("Seleccione productos pendientes para iniciar.");
                AutorizacionOperacionWindow auth = new("Autorizar inicio de producción") { Owner = this };
                if (auth.ShowDialog() != true) return;
                Usuario usuario = _negocio.Autorizar(auth.Usuario, auth.Clave);
                var items = pendientes.Select(x => { OrdenTrabajoDetalleArea fila = seleccion[x.IdDetalleOT]; decimal cantidad = fila.CantidadOperacion > 0 ? fila.CantidadOperacion : x.CantidadPlanificada; return new OrdenTrabajoLanzamiento { IdDetalleOT = x.IdDetalleOT, CantidadLanzada = cantidad, Motivo = cantidad != x.CantidadPlanificada ? "AJUSTE AUTORIZADO" : string.Empty, Observacion = auth.Observacion }; });
                _negocio.Lanzar(_id, SessionManager.UsuarioActual?.IdUsuario ?? 0, usuario, items);
                NotificationService.Success("Los productos seleccionados fueron iniciados.");
                Cargar();
            }
            catch (Exception ex) { NotificationService.Error(ex.Message); }
        }

        private void Merma_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermisoOperacion()) return;

            try
            {
                if (ItemsGrid.SelectedItem is not OrdenTrabajoDetalleArea item) throw new InvalidOperationException("Seleccione el producto donde registrará la merma.");
                if (item.CantidadOperacion <= 0) throw new InvalidOperationException("Ingrese la merma en la columna Cantidad.");
                AutorizacionOperacionWindow auth = new("Autorizar merma") { Owner = this };
                if (auth.ShowDialog() != true) return;
                if (string.IsNullOrWhiteSpace(auth.Observacion)) throw new InvalidOperationException("La merma requiere un motivo en la observación.");
                Usuario usuario = _negocio.Autorizar(auth.Usuario, auth.Clave);
                _negocio.RegistrarMerma(item.IdDetalleArea, item.CantidadOperacion, auth.Observacion, string.Empty, SessionManager.UsuarioActual?.IdUsuario ?? 0, usuario);
                NotificationService.Success("Merma registrada por producto y área.");
                Cargar();
            }
            catch (Exception ex) { NotificationService.Error(ex.Message); }
        }

        private void Consumo_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermisoOperacion()) return;

            try
            {
                if (ItemsGrid.SelectedItem is not OrdenTrabajoDetalleArea area) throw new InvalidOperationException("Seleccione el producto cuyo consumo real desea confirmar.");
                if (!ConfirmDialogService.Confirmar($"¿Confirmar el consumo real de insumos de {area.CodigoProducto}? El stock podrá quedar negativo.", "Confirmar consumo")) return;
                _negocio.ConfirmarConsumo(area.IdDetalleOT, SessionManager.UsuarioActual?.IdUsuario ?? 0);
                NotificationService.Success("Consumo registrado, stock actualizado y salida creada en kardex.");
                Cargar();
            }
            catch (Exception ex) { NotificationService.Error(ex.Message); }
        }

        private sealed class ResumenAreaCelda
        {
            public OrdenTrabajoDetalleArea Area { get; }
            public string AreaDestino { get; }
            public Brush Fondo { get; }
            public Brush TextoColor { get; }
            public bool EsPrimerProceso { get; }
            public string Ayuda => Area.EsTermino ? "Clic para ingresar a productos terminados" : Area.CantidadPendiente > 0 ? $"Clic para transferir a {AreaDestino}" : $"Sin stock disponible en {Area.NombreArea}";
            public ResumenAreaCelda(OrdenTrabajoDetalleArea area, string destino, int indice)
            {
                Area = area;
                AreaDestino = destino;
                EsPrimerProceso = indice == 0;
                bool pendiente = area.CantidadPendiente > 0;
                Color fuerte = indice % 3 == 0 ? Color.FromRgb(255, 87, 34) : indice % 3 == 1 ? Color.FromRgb(33, 150, 243) : Color.FromRgb(255, 193, 7);
                Color suave = indice % 3 == 0 ? Color.FromRgb(255, 204, 188) : indice % 3 == 1 ? Color.FromRgb(187, 222, 251) : Color.FromRgb(255, 236, 179);
                Fondo = new SolidColorBrush(pendiente ? fuerte : suave);
                TextoColor = new SolidColorBrush(pendiente ? Colors.Black : Color.FromRgb(100, 116, 139));
            }
        }

        private bool ValidarPermisoOperacion()
        {
            if (_puedeOperarOt)
                return true;

            PermissionService.MostrarSinPermiso();
            return false;
        }
    }
}
