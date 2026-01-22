using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GestorTareasWPF
{
    public partial class ConfigDialog : Window
    {
        // Lista temporal que manipulamos en la ventana
        public List<string> ColumnasResultantes { get; private set; }

        public ConfigDialog(List<string> columnasActuales)
        {
            InitializeComponent();
            // Copiamos la lista para no modificar la original hasta dar "Guardar"
            ColumnasResultantes = new List<string>(columnasActuales);
            ActualizarLista();
        }

        private void ActualizarLista()
        {
            lstColumnas.ItemsSource = null;
            lstColumnas.ItemsSource = ColumnasResultantes;
        }

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNuevaColumna.Text.Trim();
            if (!string.IsNullOrWhiteSpace(nombre) && !ColumnasResultantes.Contains(nombre))
            {
                ColumnasResultantes.Add(nombre);
                txtNuevaColumna.Clear();
                ActualizarLista();
            }
            else
            {
                MessageBox.Show("Nombre inválido o ya existente.");
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (lstColumnas.SelectedItem is string seleccionada)
            {
                ColumnasResultantes.Remove(seleccionada);
                ActualizarLista();
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (ColumnasResultantes.Count == 0)
            {
                MessageBox.Show("Debes tener al menos una columna.");
                return;
            }
            this.DialogResult = true;
            this.Close();
        }
    }
}