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
using static alglib;

namespace DiplomovaPracaLB
{

    public partial class Page_RBF : Page
    {
        private MainWindow MW;
        bool dragging;
        double param_c = 1;
        double Hardy_param;
        double Franke_param;
        double Fasshauer_param;


        double min_param_val, max_param_val;
        BASIS_FUNCTION UsedRBFType;

        public Page_RBF(MainWindow Hlavne_okno, BASIS_FUNCTION new_RBFType)
        {
            dragging = false;
            MW = Hlavne_okno;
            InitializeComponent();      //nacitanim hodnoty slidera sa spousti aj vykreslenie
            UsedRBFType = new_RBFType;

            //default params:
            double sample_size = MW.DisplayedTerrain.GetSampleSize()[0] * MW.DisplayedTerrain.GetSampleSize()[1];
            double sqrt_sample_size = Math.Sqrt(sample_size);
            Hardy_param = 0.815 * MW.DisplayedTerrain.GetAverageMinimalDistanceOfSample() / sample_size;  // chosen by Hardy 1971: 0.815 * (sum of nearest neightbour distances of N points)/N)
            Franke_param = 1.25 * MW.DisplayedTerrain.GetApproximateDiameterOfSample() / sqrt_sample_size;
            Fasshauer_param = 2 / sqrt_sample_size;

            param_c = 0.00164;

            SetUI(ref param_c);

            MW.UseRBF(UsedRBFType, param_c);

        }

        private void SetUI(ref double outParam)
        {
            TextBox_BazFcia.Text = Equation(UsedRBFType);

            //min_param_val = double.MinValue;
            //max_param_val = double.MaxValue;

            min_param_val = 0;
            max_param_val = 0.05;

            //if (min_param_val == double.MinValue) TextBox_ParamRangeMin.Text = "- inf";
            TextBox_ParamRangeMin.Text = min_param_val.ToString();
            Slider_RBFParam.Minimum = min_param_val;

            //if (max_param_val == double.MaxValue) TextBox_ParamRangeMax.Text = "inf";
            TextBox_ParamRangeMax.Text = max_param_val.ToString();
            Slider_RBFParam.Maximum = max_param_val;

            TextBox_ParamCValue.Text = outParam.ToString();
        }

        private void ChangeParameter(double new_param_value)
        {
            param_c = new_param_value;
            ReCompute();
        }

        private void ReCompute()
        {
            MW.UseRBF(UsedRBFType, param_c);
            MW.glControl.Invalidate();
        }

        private void SliderRBF_ThumbDragStarted(object sender, DragStartedEventArgs e)
        {
            dragging = true;
        }

        private void SliderRBF_ThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            dragging = false;
            ChangeParameter(Slider_RBFParam.Value);
        }
        private void Slider_RBFParam_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double new_param = e.NewValue;
            TextBox_ParamCValue.Text = Math.Round(new_param, 5).ToString();
            if (!dragging)
            {
                ChangeParameter((float)e.NewValue);
            }
        }

        private void TextBox_Parameter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)    //Enter
            {
                try
                {
                    float new_tension = float.Parse(TextBox_ParamCValue.Text);
                    if (/*Slider_RBFParam.Maximum >= new_tension &&*/ Slider_RBFParam.Minimum <= new_tension)
                    {
                        Slider_RBFParam.Value = new_tension;     //zmena sa iniciuje sliderom
                    }
                    else MessageBox.Show("Vstup mimo rozahu!");
                }
                catch
                {
                    MessageBox.Show("Nesprávny vstup!");
                }
            }
        }

        private void Button_RBFParamHardy_Click(object sender, RoutedEventArgs e)
        {
            SetUI(ref Hardy_param);
        }

        private void Button_RBFParamFranke_Click(object sender, RoutedEventArgs e)
        {
            SetUI(ref Franke_param);
        }

        private void Button_RBFParamFasshauer_Click(object sender, RoutedEventArgs e)
        {
            SetUI(ref Fasshauer_param);
        }

        private string Equation(BASIS_FUNCTION inSelectedRBF)
        {
            switch (inSelectedRBF)
            {
                default:
                case BASIS_FUNCTION.GAUSSIAN: return "e^-(d*c)\u00B2";
                case BASIS_FUNCTION.MULTIQUADRATIC: return "√(1 + c\u00B2 * d²)";
                case BASIS_FUNCTION.INVERSE_QUADRATIC: return "1/(1 + c\u00B2 * d²)";
                case BASIS_FUNCTION.INVERSE_MULTIQUADRATIC: return "1/√(1 + c\u00B2 * d²)";
                case BASIS_FUNCTION.THIN_PLATE: return "d² ln(d)";
            }
        }
    }
}
