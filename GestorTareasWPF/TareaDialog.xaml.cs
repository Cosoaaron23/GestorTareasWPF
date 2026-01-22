using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GestorTareasWPF
{
    public partial class TareaDialog : Window
    {
        // Propiedades públicas
        public string IdTarea { get; private set; } // Propiedad para el ID
        public string TituloIngresado { get; private set; }
        public string DescripcionIngresada { get; private set; }
        public string EstadoSeleccionado { get; private set; }

        // Constructor 1: CREAR NUEVA TAREA
        public TareaDialog(List<string> columnasDisponibles)
        {
            InitializeComponent();

            // Generamos un ID nuevo para mostrarlo (simulando lo que hará el modelo)
            IdTarea = Guid.NewGuid().ToString();
            txtId.Text = IdTarea;

            txtTitulo.Focus();

            // Llenar combo
            cmbEstado.ItemsSource = columnasDisponibles;
            if (columnasDisponibles.Count > 0) cmbEstado.SelectedIndex = 0;
        }

        // Constructor 2: EDITAR TAREA EXISTENTE (Ahora recibe el ID)
        public TareaDialog(string id, string titulo, string descripcion, string estadoActual, List<string> columnasDisponibles)
        {
            InitializeComponent();

            // Mostrar los datos existentes
            IdTarea = id;
            txtId.Text = id; // Mostramos el ID en la caja gris

            txtTitulo.Text = titulo;
            txtDesc.Text = descripcion;

            TituloIngresado = titulo;
            DescripcionIngresada = descripcion;
            EstadoSeleccionado = estadoActual;

            // Configurar combo
            cmbEstado.ItemsSource = columnasDisponibles;
            cmbEstado.SelectedItem = estadoActual;
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitulo.Text))
            {
                MessageBox.Show("El título es obligatorio.");
                return;
            }
            if (cmbEstado.SelectedItem == null)
            {
                MessageBox.Show("Selecciona una columna.");
                return;
            }

            // Guardamos valores para que el TaskBoard los recoja
            IdTarea = txtId.Text;
            TituloIngresado = txtTitulo.Text;
            DescripcionIngresada = txtDesc.Text;
            EstadoSeleccionado = cmbEstado.SelectedItem.ToString();

            this.DialogResult = true;
            this.Close();
        }
    }
}