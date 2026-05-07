using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.Datos.Datos;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CorexProd.WPF.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _usuario;
        private string _mensaje;

        public string Usuario
        {
            get { return _usuario; }
            set
            {
                _usuario = value;
                OnPropertyChanged();
            }
        }

        public string Mensaje
        {
            get { return _mensaje; }
            set
            {
                _mensaje = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }

        private readonly UsuarioNegocio _usuarioNegocio;

        public LoginViewModel()
        {
            _usuarioNegocio = new UsuarioNegocio();
            LoginCommand = new RelayCommand(Login);
        }

        private void Login(object parametro)
        {
            try
            {
                PasswordBox passwordBox = parametro as PasswordBox;
                string clave = passwordBox.Password;

                var usuarioLogueado =
                    _usuarioNegocio.Login(Usuario, clave);

                SessionManager.UsuarioActual = usuarioLogueado;
                PermisoMenuDatos permisoDatos =
                new PermisoMenuDatos();

                SessionManager.MenusPermitidos =
                    permisoDatos.ObtenerMenusPorRol(
                        usuarioLogueado.IdRol
                    );

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                Application.Current.Windows[0].Close();
            }
            catch (Exception ex)
            {
                Mensaje = ex.Message;
            }
        }
    }
}