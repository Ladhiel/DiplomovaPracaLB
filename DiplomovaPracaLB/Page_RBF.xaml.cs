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
        double min_param_val, max_param_val;
        BASIS_FUNCTION UsedRBFType;

        public Page_RBF(MainWindow Hlavne_okno, BASIS_FUNCTION new_RBFType)
        {
            dragging = false;
            MW = Hlavne_okno;
            InitializeComponent();      //nacitanim hodnoty slidera sa spousti aj vykreslenie
            UsedRBFType = new_RBFType;

            //default param:
            double d = 1;   //TODO
            param_c = 0.815 * d;         //chosen by Hardy 1971
            SetUIforThisRBFType(ref param_c);

            MW.UseRBF(UsedRBFType, param_c);

        }

        private void SetUIforThisRBFType(ref double outParam)
        {
            min_param_val = 0;
            max_param_val = 0.01;
            switch (UsedRBFType)
            {
                default:
                case (BASIS_FUNCTION.GAUSSIAN):
                    {
                        TextBox_BazFcia.Text = "e^-(d*c)\u00B2";
                    }
                    break;
                case BASIS_FUNCTION.MULTIQUADRATIC:
                    {
                        TextBox_BazFcia.Text = "√(1 + c\u00B2 * d²)";
                    }
                    break;
                case BASIS_FUNCTION.INVERSE_QUADRATIC:
                    {
                        TextBox_BazFcia.Text = "1/(1 + c\u00B2 * d²)";
                    }
                    break;
                case BASIS_FUNCTION.INVERSE_MULTIQUADRATIC:
                    {
                        TextBox_BazFcia.Text = "1/√(1 + c\u00B2 * d²)";
                    }
                    break;
                case BASIS_FUNCTION.THIN_PLATE:
                    {
                        //todo pridaj tvarovaci parameter
                        TextBox_BazFcia.Text = "d²&#xB7;ln(d)";
                    }
                    break;
            }

            //if (min_param_val == double.MinValue) TextBox_ParamRangeMin.Text = "- inf";
            TextBox_ParamRangeMin.Text = min_param_val.ToString();
            Slider_RBFParam.Minimum = min_param_val;

            //if (max_param_val == double.MaxValue) TextBox_ParamRangeMax.Text = "inf";
            TextBox_ParamRangeMax.Text = max_param_val.ToString();
            Slider_RBFParam.Maximum = max_param_val;

            TextBox_ParamCValue.Text = param_c.ToString();
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

        private void Button_RBFParamReset_Click(object sender, RoutedEventArgs e)
        {
            SetUIforThisRBFType(ref param_c);  //default
        }
    }
}
