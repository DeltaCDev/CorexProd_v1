using CorexProd.App.Models;
using CorexProd.App.Services;
using System.Collections.ObjectModel;

namespace CorexProd.App.Pages;

[QueryProperty(nameof(Modulo), "modulo")]
public partial class SeguridadPage : ContentPage
{
    private readonly CorexProdApiClient _api;
    private readonly ObservableCollection<SeguridadItem> _items = [];
    private readonly string[] _modulos = ["Parámetros", "Roles", "Usuarios", "Empleados"];
    private SeguridadItem? _seleccionado;

    public SeguridadPage(CorexProdApiClient api)
    {
        InitializeComponent();
        _api = api;
        Lista.ItemsSource = _items;
        ModuloPicker.ItemsSource = _modulos;
        ModuloPicker.SelectedIndex = 0;
    }

    public string Modulo
    {
        set
        {
            string modulo = Uri.UnescapeDataString(value ?? string.Empty);
            int indice = Array.FindIndex(_modulos, x => x.Equals(modulo, StringComparison.OrdinalIgnoreCase));
            if (indice >= 0 && ModuloPicker is not null)
                ModuloPicker.SelectedIndex = indice;
        }
    }

    private async void OnModuloChanged(object? sender, EventArgs e) => await CargarAsync();
    private async void OnRefresh(object? sender, EventArgs e) => await CargarAsync();
    private async void OnBuscarChanged(object? sender, TextChangedEventArgs e) => Filtrar();

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _seleccionado = e.CurrentSelection.FirstOrDefault() as SeguridadItem;
        EditarButton.IsVisible = _seleccionado is not null;
    }

    private async void OnEditarClicked(object? sender, EventArgs e)
    {
        if (_seleccionado is null) return;
        try
        {
            if (ModuloPicker.SelectedIndex == 1)
            {
                string? nombre = await DisplayPromptAsync("Editar rol", "Nombre del rol:", initialValue: _seleccionado.Titulo);
                if (string.IsNullOrWhiteSpace(nombre)) return;
                await _api.GuardarRolAsync(new(_seleccionado.Id, nombre.Trim(), true));
            }
            else if (ModuloPicker.SelectedIndex == 0)
            {
                ParametroMovil? actual = (await _api.GetParametrosAsync()).FirstOrDefault(x => x.IdParametro == _seleccionado.Id);
                if (actual is null) return;
                string? codigo = await DisplayPromptAsync("Editar parámetro", "Código:", initialValue: actual.CodigoParametro);
                string? nombre = await DisplayPromptAsync("Editar parámetro", "Nombre:", initialValue: actual.NombreParametro);
                string? valor = await DisplayPromptAsync("Editar parámetro", "Valor:", initialValue: actual.ValorParametro);
                if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre) || valor is null) return;
                await _api.GuardarParametroAsync(new(actual.IdParametro, codigo.Trim(), nombre.Trim(), valor.Trim(), actual.Descripcion, actual.Estado));
            }
            else if (ModuloPicker.SelectedIndex == 3)
            {
                EmpleadoMovil? actual = (await _api.GetEmpleadosAsync()).FirstOrDefault(x => x.IdEmpleado == _seleccionado.Id);
                if (actual is null) return;
                string? tipo = await DisplayPromptAsync("Editar empleado", "Tipo de documento:", initialValue: actual.TipoDocumento);
                string? documento = await DisplayPromptAsync("Editar empleado", "Número de documento:", initialValue: actual.NumeroDocumento);
                string? nombre = await DisplayPromptAsync("Editar empleado", "Nombres:", initialValue: actual.Nombre);
                string? apellido = await DisplayPromptAsync("Editar empleado", "Apellidos:", initialValue: actual.Apellido);
                CargoMovil? cargo = await SeleccionarCargoAsync(await _api.GetCargosAsync(), actual.IdCargo);
                if (string.IsNullOrWhiteSpace(documento) || string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) || cargo is null) return;
                await _api.GuardarEmpleadoAsync(new(actual.IdEmpleado, tipo ?? actual.TipoDocumento, documento.Trim(), nombre.Trim(), apellido.Trim(), actual.Sexo, actual.Telefono, actual.Email, actual.Direccion, cargo.IdCargo, actual.FechaNacimiento, actual.Estado));
            }
            else if (ModuloPicker.SelectedIndex == 2)
            {
                UsuarioMovil? actual = (await _api.GetUsuariosAsync()).FirstOrDefault(x => x.IdUsuario == _seleccionado.Id);
                if (actual is null) return;
                EmpleadoMovil? empleado = await SeleccionarEmpleadoAsync(await _api.GetEmpleadosAsync(), actual.IdEmpleado);
                RolMovil? rol = await SeleccionarRolAsync(await _api.GetRolesAsync(), actual.IdRol);
                string? usuario = await DisplayPromptAsync("Editar usuario", "Nombre de usuario:", initialValue: actual.NombreUsuario);
                string? clave = await DisplayPromptAsync("Editar usuario", "Nueva contraseña (opcional):", keyboard: Keyboard.Text);
                if (empleado is null || rol is null || string.IsNullOrWhiteSpace(usuario)) return;
                await _api.GuardarUsuarioAsync(new(actual.IdUsuario, empleado.IdEmpleado, usuario.Trim(), string.IsNullOrWhiteSpace(clave) ? null : clave, rol.IdRol, actual.Estado));
            }

            await DisplayAlertAsync("Seguridad", "Registro actualizado correctamente.", "OK");
            await CargarAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Seguridad", ex.Message, "OK");
        }
    }

    private async void OnNuevoClicked(object? sender, EventArgs e)
    {
        try
        {
            if (ModuloPicker.SelectedIndex == 1)
            {
                string? nombre = await DisplayPromptAsync("Nuevo rol", "Nombre del rol:");
                if (string.IsNullOrWhiteSpace(nombre)) return;
                await _api.GuardarRolAsync(new(0, nombre.Trim(), true));
            }
            else if (ModuloPicker.SelectedIndex == 0)
            {
                string? codigo = await DisplayPromptAsync("Nuevo parámetro", "Código:");
                if (string.IsNullOrWhiteSpace(codigo)) return;
                string? nombre = await DisplayPromptAsync("Nuevo parámetro", "Nombre:");
                if (string.IsNullOrWhiteSpace(nombre)) return;
                string? valor = await DisplayPromptAsync("Nuevo parámetro", "Valor:");
                if (valor is null) return;
                await _api.GuardarParametroAsync(new(0, codigo.Trim(), nombre.Trim(), valor.Trim(), string.Empty, true));
            }
            else if (ModuloPicker.SelectedIndex == 3)
            {
                string? tipo = await DisplayPromptAsync("Nuevo empleado", "Tipo de documento:", initialValue: "DNI");
                string? documento = await DisplayPromptAsync("Nuevo empleado", "Número de documento:");
                string? nombre = await DisplayPromptAsync("Nuevo empleado", "Nombres:");
                string? apellido = await DisplayPromptAsync("Nuevo empleado", "Apellidos:");
                IReadOnlyList<CargoMovil> cargos = await _api.GetCargosAsync();
                CargoMovil? cargo = await SeleccionarCargoAsync(cargos);
                if (string.IsNullOrWhiteSpace(documento) || string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido)
                    || cargo is null) return;
                await _api.GuardarEmpleadoAsync(new(0, tipo ?? "DNI", documento, nombre, apellido, "", "", "", "", cargo.IdCargo, null, true));
            }
            else if (ModuloPicker.SelectedIndex == 2)
            {
                IReadOnlyList<EmpleadoMovil> empleados = await _api.GetEmpleadosAsync();
                IReadOnlyList<RolMovil> roles = await _api.GetRolesAsync();
                EmpleadoMovil? empleado = await SeleccionarEmpleadoAsync(empleados);
                RolMovil? rol = await SeleccionarRolAsync(roles);
                string? usuario = await DisplayPromptAsync("Nuevo usuario", "Nombre de usuario:");
                string? clave = await DisplayPromptAsync("Nuevo usuario", "Contraseña:", keyboard: Keyboard.Text);
                if (empleado is null || rol is null || string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave)) return;
                await _api.GuardarUsuarioAsync(new(0, empleado.IdEmpleado, usuario, clave, rol.IdRol, true));
            }
            else
            {
                return;
            }

            await DisplayAlertAsync("Seguridad", "Registro guardado correctamente.", "OK");
            await CargarAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Seguridad", ex.Message, "OK");
        }
    }

    private async Task<CargoMovil?> SeleccionarCargoAsync(IReadOnlyList<CargoMovil> cargos)
        => await SeleccionarCargoAsync(cargos, null);

    private async Task<CargoMovil?> SeleccionarCargoAsync(IReadOnlyList<CargoMovil> cargos, int? seleccionado)
    {
        if (cargos.Count == 0) return null;
        string? opcion = await DisplayActionSheetAsync("Seleccione el cargo", "Cancelar", null, cargos.Select(x => $"{x.IdCargo} - {x.NombreCargo}" + (x.IdCargo == seleccionado ? " (actual)" : string.Empty)).ToArray());
        if (opcion is not null) opcion = opcion.Replace(" (actual)", string.Empty, StringComparison.Ordinal);
        return cargos.FirstOrDefault(x => $"{x.IdCargo} - {x.NombreCargo}" == opcion);
    }

    private async Task<EmpleadoMovil?> SeleccionarEmpleadoAsync(IReadOnlyList<EmpleadoMovil> empleados)
        => await SeleccionarEmpleadoAsync(empleados, null);

    private async Task<EmpleadoMovil?> SeleccionarEmpleadoAsync(IReadOnlyList<EmpleadoMovil> empleados, int? seleccionado)
    {
        if (empleados.Count == 0) return null;
        string? opcion = await DisplayActionSheetAsync("Seleccione el empleado", "Cancelar", null, empleados.Select(x => $"{x.IdEmpleado} - {x.NombreCompleto}" + (x.IdEmpleado == seleccionado ? " (actual)" : string.Empty)).ToArray());
        if (opcion is not null) opcion = opcion.Replace(" (actual)", string.Empty, StringComparison.Ordinal);
        return empleados.FirstOrDefault(x => $"{x.IdEmpleado} - {x.NombreCompleto}" == opcion);
    }

    private async Task<RolMovil?> SeleccionarRolAsync(IReadOnlyList<RolMovil> roles)
        => await SeleccionarRolAsync(roles, null);

    private async Task<RolMovil?> SeleccionarRolAsync(IReadOnlyList<RolMovil> roles, int? seleccionado)
    {
        if (roles.Count == 0) return null;
        string? opcion = await DisplayActionSheetAsync("Seleccione el rol", "Cancelar", null, roles.Select(x => $"{x.IdRol} - {x.NombreRol}" + (x.IdRol == seleccionado ? " (actual)" : string.Empty)).ToArray());
        if (opcion is not null) opcion = opcion.Replace(" (actual)", string.Empty, StringComparison.Ordinal);
        return roles.FirstOrDefault(x => $"{x.IdRol} - {x.NombreRol}" == opcion);
    }

    private async void OnPermisosClicked(object? sender, EventArgs e)
    {
        string? idTexto = await DisplayPromptAsync("Permisos", "Ingrese el Id del rol:");
        if (!int.TryParse(idTexto, out int idRol) || idRol <= 0)
            return;

        try
        {
            IReadOnlyList<PermisoRolMovil> permisos = await _api.GetPermisosRolAsync(idRol);
            string resumen = string.Join("\n", permisos.Select(x => $"{(x.PuedeVer ? "✓" : "✗")} {x.NombreMenu}"));
            bool guardar = await DisplayAlertAsync("Permisos del rol", resumen, "Habilitar todos", "Cerrar");
            if (guardar)
            {
                await _api.GuardarPermisosRolAsync(idRol, permisos.Select(x => new PermisoRolGuardar(x.IdMenu, true)).ToList());
                await DisplayAlertAsync("Permisos", "Permisos actualizados correctamente.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Permisos", ex.Message, "OK");
        }
    }

    private async Task CargarAsync()
    {
        if (ModuloPicker.SelectedIndex < 0) return;
        Cargando.IsVisible = true;
        Cargando.IsRunning = true;
        Refrescar.IsRefreshing = false;
        try
        {
            _items.Clear();
            _seleccionado = null;
            EditarButton.IsVisible = false;
            Lista.SelectedItem = null;
            PermisosButton.IsVisible = ModuloPicker.SelectedIndex == 1;
            switch (ModuloPicker.SelectedIndex)
            {
                case 0:
                    foreach (ParametroMovil x in await _api.GetParametrosAsync())
                        _items.Add(new(x.IdParametro, x.NombreParametro, $"Código: {x.CodigoParametro} | Valor: {x.ValorParametro}", x.Estado ? "Activo" : "Inactivo"));
                    break;
                case 1:
                    foreach (RolMovil x in await _api.GetRolesAsync())
                        _items.Add(new(x.IdRol, x.NombreRol, $"Registrado: {x.FechaRegistro:dd/MM/yyyy}", x.Estado ? "Activo" : "Inactivo"));
                    break;
                case 2:
                    foreach (UsuarioMovil x in await _api.GetUsuariosAsync())
                        _items.Add(new(x.IdUsuario, x.NombreUsuario, $"{x.NombreCompleto} | Rol: {x.NombreRol}", x.Estado ? "Activo" : "Inactivo"));
                    break;
                case 3:
                    foreach (EmpleadoMovil x in await _api.GetEmpleadosAsync())
                        _items.Add(new(x.IdEmpleado, x.NombreCompleto, $"{x.TipoDocumento}: {x.NumeroDocumento} | Cargo: {x.NombreCargo}", x.Estado ? "Activo" : "Inactivo"));
                    break;
            }
            Filtrar();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Seguridad", ex.Message, "OK");
        }
        finally
        {
            Cargando.IsRunning = false;
            Cargando.IsVisible = false;
            Refrescar.IsRefreshing = false;
        }
    }

    private void Filtrar()
    {
        string texto = BuscarEntry.Text?.Trim() ?? string.Empty;
        Lista.ItemsSource = string.IsNullOrWhiteSpace(texto)
            ? _items
            : _items.Where(x => $"{x.Titulo} {x.Detalle} {x.Estado}".Contains(texto, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private sealed record SeguridadItem(int Id, string Titulo, string Detalle, string Estado);
}
