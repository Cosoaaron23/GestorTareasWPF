using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GestorTareasWPF
{
    public partial class FrmInformeKanban : Window
    {
        // Esta lista SIEMPRE tiene todas las tareas (la copia maestra)
        private readonly List<TareaModel> _todasLasTareasMaster;

        // Esta lista es la que se está mostrando actualmente (puede estar filtrada)
        private List<TareaModel> _tareasVisualizadas;

        private int _tipoGraficoActual = 0;

        public FrmInformeKanban(List<TareaModel> tareas)
        {
            InitializeComponent();

            // Guardamos la copia original
            _todasLasTareasMaster = tareas ?? new List<TareaModel>();

            // Al principio, mostramos todas
            _tareasVisualizadas = new List<TareaModel>(_todasLasTareasMaster);

            // Cargamos la vista inicial
            ActualizarPantalla(_tareasVisualizadas);
        }

        // ---------------------------------------------------------
        // 1. LÓGICA DE FILTRADO (NUEVO)
        // ---------------------------------------------------------
        private void CmbFiltro_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // --- CORRECCIÓN AQUÍ ---
            // Si la lista maestra aún no se ha cargado (pasa al abrir la ventana), no hagas nada.
            if (_todasLasTareasMaster == null) return;
            // -----------------------

            if (cmbFiltro.SelectedItem is ComboBoxItem item)
            {
                string filtro = item.Content.ToString();

                if (filtro == "Todas")
                {
                    // Restauramos la lista completa
                    _tareasVisualizadas = new List<TareaModel>(_todasLasTareasMaster);
                }
                else
                {
                    string palabraClave = filtro.ToLower();
                    if (palabraClave == "en proceso") palabraClave = "proceso";

                    _tareasVisualizadas = _todasLasTareasMaster
                        .Where(t => (t.Estado?.ToLower().Contains(palabraClave) ?? false)) // El ?? false protege de nulos
                        .ToList();
                }

                ActualizarPantalla(_tareasVisualizadas);
            }
        }

        // ---------------------------------------------------------
        // 2. MÉTODO CENTRAL DE ACTUALIZACIon
        // ---------------------------------------------------------
        private void ActualizarPantalla(List<TareaModel> tareasParaMostrar)
        {
            // A. Actualizar KPIs (Tarjetas de arriba)
            int total = tareasParaMostrar.Count;

            // Calculamos completadas SOBRE LA LISTA ACTUAL
            int completadas = tareasParaMostrar.Count(t =>
                (t.Estado?.ToLower().Contains("completado") ?? false) ||
                (t.Estado?.ToLower().Contains("done") ?? false) ||
                (t.Estado?.ToLower().Contains("hecho") ?? false));

            double porcentaje = total == 0 ? 0 : (double)completadas / total * 100;

            txtTotal.Text = total.ToString();
            txtPorcentaje.Text = $"{porcentaje:F1}%";

            // B. Actualizar Tabla
            gridDetalle.ItemsSource = null; // Reset necesario a veces para refrescar
            gridDetalle.ItemsSource = tareasParaMostrar;

            // C. Actualizar Gráfico
            ActualizarGrafico(tareasParaMostrar);
        }

        // ---------------------------------------------------------
        // 3. LÓGICA DE GRÁFICOS
        // ---------------------------------------------------------
        private void BtnCambiarGrafico_Click(object sender, RoutedEventArgs e)
        {
            _tipoGraficoActual++;
            if (_tipoGraficoActual > 4) _tipoGraficoActual = 0;

            // Usamos la lista actual (_tareasVisualizadas) para redibujar
            ActualizarGrafico(_tareasVisualizadas);
        }

        private void ActualizarGrafico(List<TareaModel> datos)
        {
            var grupos = datos.GroupBy(t => t.Estado);
            SeriesCollection series = new SeriesCollection();

            switch (_tipoGraficoActual)
            {
                case 0: // PIE
                    MostrarCircular(true);
                    graficoCircular.InnerRadius = 0;
                    foreach (var g in grupos)
                        series.Add(new PieSeries { Title = g.Key, Values = new ChartValues<int> { g.Count() }, DataLabels = true });
                    graficoCircular.Series = series;
                    break;

                case 1: // DONUT
                    MostrarCircular(true);
                    graficoCircular.InnerRadius = 60;
                    foreach (var g in grupos)
                        series.Add(new PieSeries { Title = g.Key, Values = new ChartValues<int> { g.Count() }, DataLabels = true });
                    graficoCircular.Series = series;
                    break;

                case 2: // COLUMNAS
                    MostrarCircular(false);
                    ConfigurarEjes(false);
                    foreach (var g in grupos)
                        series.Add(new ColumnSeries { Title = g.Key, Values = new ChartValues<int> { g.Count() }, DataLabels = true });
                    graficoCartesiano.Series = series;
                    break;

                case 3: // FILAS
                    MostrarCircular(false);
                    ConfigurarEjes(true);
                    foreach (var g in grupos)
                        series.Add(new RowSeries { Title = g.Key, Values = new ChartValues<int> { g.Count() }, DataLabels = true });
                    graficoCartesiano.Series = series;
                    break;

                case 4: // LINEAS
                    MostrarCircular(false);
                    ConfigurarEjes(false);
                    foreach (var g in grupos)
                        series.Add(new LineSeries { Title = g.Key, Values = new ChartValues<int> { g.Count() }, DataLabels = true, PointGeometrySize = 15 });
                    graficoCartesiano.Series = series;
                    break;
            }
        }

        private void MostrarCircular(bool visible)
        {
            if (visible)
            {
                graficoCircular.Visibility = Visibility.Visible;
                graficoCartesiano.Visibility = Visibility.Hidden;
            }
            else
            {
                graficoCircular.Visibility = Visibility.Hidden;
                graficoCartesiano.Visibility = Visibility.Visible;
            }
        }

        private void ConfigurarEjes(bool esHorizontal)
        {
            graficoCartesiano.AxisX.Clear();
            graficoCartesiano.AxisY.Clear();

            if (esHorizontal)
            {
                graficoCartesiano.AxisX.Add(new Axis { Title = "Cantidad", MinValue = 0 });
                graficoCartesiano.AxisY.Add(new Axis { Labels = new[] { "" } });
            }
            else
            {
                graficoCartesiano.AxisY.Add(new Axis { Title = "Cantidad", MinValue = 0 });
                graficoCartesiano.AxisX.Add(new Axis { Labels = new[] { "" } });
            }
        }

        // ---------------------------------------------------------
        // 4. EXPORTAR A PDF (Se mantiene igual)
        // ---------------------------------------------------------
        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Archivo PDF (*.pdf)|*.pdf";
                saveFileDialog.FileName = "Informe_Kanban_" + DateTime.Now.ToString("yyyyMMdd_HHmm");

                if (saveFileDialog.ShowDialog() == true)
                {
                    if (PanelBotones != null) PanelBotones.Visibility = Visibility.Hidden;
                    this.UpdateLayout();

                    double width = this.ActualWidth;
                    double height = this.ActualHeight;

                    RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)width, (int)height, 96d, 96d, PixelFormats.Pbgra32);
                    renderBitmap.Render(this);

                    PdfDocument document = new PdfDocument();
                    PdfPage page = document.AddPage();
                    page.Width = width;
                    page.Height = height;

                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    using (MemoryStream stream = new MemoryStream())
                    {
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                        encoder.Save(stream);
                        XImage img = XImage.FromStream(stream);
                        gfx.DrawImage(img, 0, 0, width, height);
                    }

                    document.Save(saveFileDialog.FileName);

                    if (PanelBotones != null) PanelBotones.Visibility = Visibility.Visible;
                    MessageBox.Show($"Informe guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                if (PanelBotones != null) PanelBotones.Visibility = Visibility.Visible;
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}