using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GestorTareasWPF
{
    public partial class TaskBoard : UserControl
    {
        private const string ArchivoTareas = "tareas.json";
        private const string ArchivoConfig = "config.json";

        private List<string> _nombresColumnas = new List<string>();

        // Diccionario para controlar las columnas visuales
        private Dictionary<string, StackPanel> _mapaColumnasUI = new Dictionary<string, StackPanel>();

        public TaskBoard()
        {
            InitializeComponent();
            CargarConfiguracion();
            RenderizarTablero();
            CargarTareas();
        }

        // ---------------------------------------------------------
        // 1. INTEGRACIÓN CON EL INFORME (Requisito PDF)
        // ---------------------------------------------------------
        private void BtnInforme_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Recopilamos todas las tareas actuales de todas las columnas para enviarlas al informe
                var listaTotal = new List<TareaModel>();

                foreach (var kvp in _mapaColumnasUI)
                {
                    string nombreEstado = kvp.Key;
                    StackPanel panel = kvp.Value;

                    foreach (var child in panel.Children)
                    {
                        if (child is TaskItem item)
                        {
                            // Aseguramos que el modelo tenga el estado actualizado
                            item.TareaDatos.Estado = nombreEstado;
                            listaTotal.Add(item.TareaDatos);
                        }
                    }
                }

                // Abrimos la nueva ventana de informe
                FrmInformeKanban informe = new FrmInformeKanban(listaTotal);
                informe.Owner = Window.GetWindow(this); // Opcional: Centrar sobre la ventana actual
                informe.ShowDialog();
            }
            catch (Exception ex)
            {
                // Requisito: Manejo de excepciones para no bloquear la app
                MessageBox.Show($"No se pudo generar el informe.\nDetalle: {ex.Message}",
                                "Error de Informe", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------------------------------------------------
        // 2. CONFIGURACIÓN Y RENDERIZADO
        // ---------------------------------------------------------
        private void BtnConfigurar_Click(object sender, RoutedEventArgs e)
        {
            ConfigDialog dialog = new ConfigDialog(_nombresColumnas);
            if (dialog.ShowDialog() == true)
            {
                _nombresColumnas = dialog.ColumnasResultantes;
                GuardarConfiguracion();
                RenderizarTablero();
                CargarTareas();
            }
        }

        private void RenderizarTablero()
        {
            gridColumnas.Children.Clear();
            gridColumnas.ColumnDefinitions.Clear();
            _mapaColumnasUI.Clear();

            for (int i = 0; i < _nombresColumnas.Count; i++)
            {
                string nombreCol = _nombresColumnas[i];

                gridColumnas.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });

                Border borde = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                    Margin = new Thickness(5),
                    CornerRadius = new CornerRadius(5)
                };
                Grid.SetColumn(borde, i);

                Grid gridInterno = new Grid();
                gridInterno.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                gridInterno.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                // Título de Columna
                TextBlock titulo = new TextBlock
                {
                    Text = nombreCol.ToUpper(),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10)
                };
                gridInterno.Children.Add(titulo);

                ScrollViewer scrollVertical = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                Grid.SetRow(scrollVertical, 1);

                StackPanel panelItems = new StackPanel
                {
                    MinHeight = 400,
                    AllowDrop = true,
                    Background = Brushes.Transparent,
                    Tag = nombreCol
                };

                panelItems.Drop += Columna_Drop;

                scrollVertical.Content = panelItems;
                gridInterno.Children.Add(scrollVertical);
                borde.Child = gridInterno;
                gridColumnas.Children.Add(borde);

                _mapaColumnasUI[nombreCol] = panelItems;
            }
        }

        // ---------------------------------------------------------
        // 3. GESTIÓN DE TAREAS (Crear, Editar, Mover)
        // ---------------------------------------------------------
        private void BtnNuevaTarea_Click(object sender, RoutedEventArgs e)
        {
            TareaDialog dialog = new TareaDialog(_nombresColumnas);

            if (dialog.ShowDialog() == true)
            {
                // Asumiendo que has actualizado TareaModel con Prioridad y Responsable
                var nuevaTarea = new TareaModel
                {
                    Id = dialog.IdTarea,
                    Titulo = dialog.TituloIngresado,
                    Descripcion = dialog.DescripcionIngresada,
                    Estado = dialog.EstadoSeleccionado,
                    // Valores por defecto si tu diálogo aún no los pide
                    Prioridad = "Media",
                    Responsable = "Sin asignar"
                };

                AgregarTareaVisual(nuevaTarea);
                GuardarTareas();
            }
        }

        private void AgregarTareaVisual(TareaModel tarea)
        {
            if (!_mapaColumnasUI.ContainsKey(tarea.Estado)) return;

            var item = new TaskItem(tarea);

            item.EliminarClicked += (s, a) => {
                if (item.Parent is Panel padre)
                {
                    padre.Children.Remove(item);
                    GuardarTareas();
                }
            };

            item.EditarClicked += (s, a) => EditarTarea(item);

            _mapaColumnasUI[tarea.Estado].Children.Add(item);
        }

        private void EditarTarea(TaskItem item)
        {
            string estadoOriginal = item.TareaDatos.Estado;

            TareaDialog dialog = new TareaDialog(
                item.TareaDatos.Id,
                item.TareaDatos.Titulo,
                item.TareaDatos.Descripcion,
                estadoOriginal,
                _nombresColumnas);

            if (dialog.ShowDialog() == true)
            {
                item.TareaDatos.Titulo = dialog.TituloIngresado;
                item.TareaDatos.Descripcion = dialog.DescripcionIngresada;
                item.TareaDatos.Estado = dialog.EstadoSeleccionado;

                item.ActualizarVisual();

                if (estadoOriginal != dialog.EstadoSeleccionado)
                {
                    if (item.Parent is Panel padre) padre.Children.Remove(item);

                    if (_mapaColumnasUI.ContainsKey(dialog.EstadoSeleccionado))
                    {
                        _mapaColumnasUI[dialog.EstadoSeleccionado].Children.Add(item);
                    }
                }
                GuardarTareas();
            }
        }

        private void Columna_Drop(object sender, DragEventArgs e)
        {
            if (sender is StackPanel panelDestino && e.Data.GetDataPresent(typeof(TaskItem)))
            {
                var item = e.Data.GetData(typeof(TaskItem)) as TaskItem;

                if (item != null)
                {
                    var padreAnterior = item.Parent as Panel;
                    padreAnterior?.Children.Remove(item);

                    panelDestino.Children.Add(item);

                    if (panelDestino.Tag != null)
                    {
                        item.TareaDatos.Estado = panelDestino.Tag.ToString();
                    }
                    GuardarTareas();
                }
            }
        }

        // ---------------------------------------------------------
        // 4. PERSISTENCIA (JSON)
        // ---------------------------------------------------------
        private void GuardarTareas()
        {
            var listaTotal = new List<TareaModel>();
            foreach (var kvp in _mapaColumnasUI)
            {
                foreach (TaskItem item in kvp.Value.Children)
                {
                    // Actualizamos estado antes de guardar por seguridad
                    item.TareaDatos.Estado = kvp.Key;
                    listaTotal.Add(item.TareaDatos);
                }
            }
            string json = JsonSerializer.Serialize(listaTotal, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ArchivoTareas, json);
        }

        private void CargarTareas()
        {
            if (File.Exists(ArchivoTareas))
            {
                try
                {
                    string json = File.ReadAllText(ArchivoTareas);
                    var lista = JsonSerializer.Deserialize<List<TareaModel>>(json);
                    if (lista != null) foreach (var t in lista) AgregarTareaVisual(t);
                }
                catch { }
            }
        }

        private void GuardarConfiguracion()
        {
            string json = JsonSerializer.Serialize(_nombresColumnas);
            File.WriteAllText(ArchivoConfig, json);
        }

        private void CargarConfiguracion()
        {
            if (File.Exists(ArchivoConfig))
            {
                try
                {
                    string json = File.ReadAllText(ArchivoConfig);
                    _nombresColumnas = JsonSerializer.Deserialize<List<string>>(json);
                }
                catch { _nombresColumnas = new List<string>(); }
            }

            if (_nombresColumnas == null || _nombresColumnas.Count == 0)
            {
                _nombresColumnas = new List<string> { "Pendiente", "En Proceso", "Completado" };
            }
        }
    }
}