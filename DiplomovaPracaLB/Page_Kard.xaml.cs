using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DiplomovaPracaLB
{
    /// <summary>
    /// Interaction logic for Page_Kard.xaml
    /// </summary>
    public partial class Page_Kard : Page
    {
        private MainWindow MW;
        //private float tension;
        bool dragging;

        public Page_Kard( MainWindow Hlavne_okno)
        {
            dragging = false;
            MW = Hlavne_okno;
            InitializeComponent();      //nacitanim hodnoty slidera sa spousti aj vykreslenie
            MW.UseKardBicubic(0, MW.LevelOfDetail);
        }

        //zdroj spustenia slideru pri pusteni bezca
        //https://stackoverflow.com/questions/723502/wpf-slider-with-an-event-that-triggers-after-a-user-drags
        private void SliderKard_ThumbDragStarted(object sender, DragStartedEventArgs e)
        {
            dragging = true;
        }

        private void SliderKard_ThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            dragging = false;
            ChangeTensionparameter((float)Slider_KardTension.Value);
        }

        private void Slider_KardTension_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float new_tension = (float)e.NewValue;
            TextBox_TensionTValue.Text = new_tension.ToString();
            TextBox_TensionSValue.Text = ((1 - new_tension) / 2).ToString();

            if (!dragging)
            {
                ChangeTensionparameter((float)e.NewValue);
            }
        }
        private void ChangeTensionparameter(float new_tension)
        {
            //TD.UseKardBilin(new_tension, MW.LevelOfDetail);
            MW.UseKardBicubic(new_tension, MW.LevelOfDetail);
            MW.glControl.Invalidate();

        }

        private void Button_CatmullRom_Click(object sender, RoutedEventArgs e)
        {
            Slider_KardTension.Value = 0.0f;    //zmena sa iniciuje sliderom
        }

        private void TextBox_TensionValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)    //Enter
            {
                float new_tension;
                try
                {
                    new_tension = float.Parse(TextBox_TensionTValue.Text);
                    if (Slider_KardTension.Maximum >= new_tension && Slider_KardTension.Minimum <= new_tension)
                    {
                        Slider_KardTension.Value = new_tension;     //zmena sa iniciuje sliderom
                    }
                    else MessageBox.Show("Vstup mimo rozahu!");
                }
                catch
                {
                    MessageBox.Show("Nesprávny vstup!");
                }
            }
        }
    }
}
