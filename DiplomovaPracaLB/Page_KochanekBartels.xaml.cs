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
    /// Interaction logic for Page_KochanekBartels.xaml
    /// </summary>
    public partial class Page_KochanekBartels : Page
    {
        private MainWindow MW;
        private float tension, continuity, bias;
        bool dragging;

        public Page_KochanekBartels(MainWindow Hlavne_okno)
        {
            dragging = false;
            MW = Hlavne_okno;
            InitializeComponent();      //nacitanim hodnoty slidera sa spousti aj vykreslenie

            float default_tension = 0.0f;
            float default_continuity = 0.0f;
            float default_bias = 0.0f;

            MW.UseKochanekBartels(default_tension, default_continuity, default_bias);

        }

        //-----fcie--------------
        private void ChangeTensionParameter(float new_tension)
        {
            tension = new_tension;
            ReCompute();
        }

        private void ChangeContinuityParameter(float new_con)
        {
            continuity = new_con;
            ReCompute();
        }

        private void ChangeBiasParameter(float new_bias)
        {
            bias = new_bias;
            ReCompute();
        }

        private void ReCompute()
        {
            MW.UseKochanekBartels(tension, continuity, bias);
            MW.glControl.Invalidate();
        }

        //----Slider-------------------
        private void Slider_Tension_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float new_tension = (float)e.NewValue;
            TextBox_Tension.Text = Math.Round(new_tension, 5).ToString();
            if (!dragging)
            {
                ChangeTensionParameter((float)e.NewValue);
            }
        }

        private void Slider_Continuity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float new_con = (float)e.NewValue;
            TextBox_Continuity.Text = Math.Round(new_con, 5).ToString();
            if (!dragging)
            {
                ChangeContinuityParameter((float)e.NewValue);
            }
        }
        private void Slider_Bias_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float new_bias = (float)e.NewValue;
            TextBox_Bias.Text = Math.Round(new_bias, 5).ToString();
            if (!dragging)
            {
                ChangeBiasParameter((float)e.NewValue);
            }
        }




        //-----Thumb---------------------
        private void Slider_ThumbDragStarted(object sender, DragStartedEventArgs e)
        {
            dragging = true;
        }

        private void SliderTension_ThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            dragging = false;
            ChangeTensionParameter((float)Slider_Tension.Value);
        }

        private void SliderContinuity_ThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            dragging = false;
            ChangeContinuityParameter((float)Slider_Continuity.Value);
        }

        private void SliderBias_ThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            dragging = false;
            ChangeBiasParameter((float)Slider_Bias.Value);
        }

        //------TextBox-------------------
        private void TextBox_Tension_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)    //Enter
            {
                try
                {
                    float new_tension = float.Parse(TextBox_Tension.Text);
                    if (Slider_Tension.Maximum >= new_tension && Slider_Tension.Minimum <= new_tension)
                    {
                        Slider_Tension.Value = new_tension;     //zmena sa iniciuje sliderom
                    }
                    else MessageBox.Show("Vstup mimo rozahu!");
                }
                catch
                {
                    MessageBox.Show("Nesprávny vstup!");
                }
            }
        }

        private void TextBox_Continuity_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)    //Enter
            {
                try
                {
                    float new_con = float.Parse(TextBox_Continuity.Text);
                    if (Slider_Continuity.Maximum >= new_con && Slider_Continuity.Minimum <= new_con)
                    {
                        Slider_Continuity.Value = new_con;     //zmena sa iniciuje sliderom
                    }
                    else MessageBox.Show("Vstup mimo rozahu!");
                }
                catch
                {
                    MessageBox.Show("Nesprávny vstup!");
                }
            }
        }

        private void TextBox_Bias_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)    //Enter
            {
                try
                {
                    float new_bias = float.Parse(TextBox_Bias.Text);
                    if (Slider_Bias.Maximum >= new_bias && Slider_Bias.Minimum <= new_bias)
                    {
                        Slider_Bias.Value = new_bias;     //zmena sa iniciuje sliderom
                    }
                    else MessageBox.Show("Vstup mimo rozahu!");
                }
                catch
                {
                    MessageBox.Show("Nesprávny vstup!");
                }
            }
        }

        //-----Button--------------
        private void Button_CatmullRom_Click(object sender, RoutedEventArgs e)
        {
            Slider_Tension.Value = 0.0f;    //zmena sa iniciuje sliderom
        }


    }
}
