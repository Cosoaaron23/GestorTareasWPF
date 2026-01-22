using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestorTareasWPF
{
    public partial class TaskItem : UserControl
    {
        public TareaModel TareaDatos { get; private set; }

        // Eventos
        public event RoutedEventHandler EliminarClicked;
        public event RoutedEventHandler EditarClicked;

        public TaskItem(TareaModel tarea)
        {
            InitializeComponent();
            this.TareaDatos = tarea;
            ActualizarVisual(); // Importante: Cargar texto al iniciar

            this.MouseLeftButtonDown += TaskItem_MouseLeftButtonDown;
        }

        public void ActualizarVisual()
        {
            if (TareaDatos != null)
            {
                lblTitulo.Text = TareaDatos.Titulo;
                lblDesc.Text = TareaDatos.Descripcion;
            }
        }

        private void TaskItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Solo arrastrar si no clickeamos botones
            if (!(e.OriginalSource is Button))
            {
                DragDrop.DoDragDrop(this, this, DragDropEffects.Move);
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            EditarClicked?.Invoke(this, e);
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            EliminarClicked?.Invoke(this, e);
        }
    }
}