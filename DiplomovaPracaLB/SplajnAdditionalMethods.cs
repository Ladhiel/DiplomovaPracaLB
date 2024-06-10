using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using swf = System.Windows.Forms;   //povenoala som kvoli konfliktnemu pomenovanius
using System.Windows.Forms.Integration;
using System.IO;
using sd = System.Drawing;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Windows.Media.Animation;
using System.Numerics;
using System.Xml.Linq;
using g3;

namespace DiplomovaPracaLB
{
    public partial class Splajn
    {
        public Vector4 MyMultiply(Vector4 v, Matrix4 M)
        {
            //vektor je riadok
            //nasobenie vektora maticou sprava
            Vector4 u = new Vector4();
            for (int i = 0; i < 4; i++)
            {
                u[i] = v[0] * M[0, i] + v[1] * M[1, i] + v[2] * M[2, i] + v[3] * M[3, i];
            }
            return u;
        }

        public Vector3 MyMultiply(Vector3 v, Matrix3 M)
        {
            //vektor je riadok
            //nasobenie vektora    maticou sprava   v*M
            Vector3 u = new Vector3();
            for (int i = 0; i < 3; i++)
            {
                u[i] = v[0] * M[0, i] + v[1] * M[1, i] + v[2] * M[2, i];
            }
            return u; //vysledny je akoby riadkovy
        }

        public Vector4 MyMultiply(Matrix4 M, Vector4 v)
        {
            //vektor je riadok
            //nasobenie stlpcoveho vektora   maticou zlava   M*v
            Vector4 u = new Vector4();
            for (int i = 0; i < 4; i++)
            {
                u[i] = M[i, 0] * v[0] + M[i, 1] * v[1] + M[i, 2] * v[2] + M[i, 3] * v[3];

            }
            return u;   //vysledny je akoby stlpcovy
        }

        public Vector3 MyMultiply(Matrix3 M, Vector3 v)
        {
            //vektor je riadok
            //nasobenie stlpcoveho vektora   maticou zlava   M*v
            Vector3 u = new Vector3();
            for (int i = 0; i < 3; i++)
            {
                u[i] = M[i, 0] * v[0] + M[i, 1] * v[1] + M[i, 2] * v[2];

            }
            return u;   //vysledny je akoby stlpcovy
        }

        public float MyMultiply(Vector3 u, Matrix3 M, Vector3 v)
        {
            //vysledok je cislo
            Vector3 uM = MyMultiply(u, M);
            float uMv = Vector3.Dot(uM, v); //skalarny sucin
            return uMv;
        }

        public float MyMultiply(Vector4 u, Matrix4 M, Vector4 v)
        {
            //vysledok je cislo
            Vector4 uM = MyMultiply(u, M);
            float uMv = Vector4.Dot(uM, v); //skalarny sucin
            return uMv;
        }

        public Matrix3 MyMultiply(Matrix3 M, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            return Matrix3.Mult(M, new Matrix3(v0, v1, v2));
        }

        public Matrix4 MyMultiply(Matrix4 M, Vector4 v0, Vector4 v1, Vector4 v2, Vector4 v3)
        {
            return Matrix4.Mult(M, new Matrix4(v0, v1, v2, v3));
        }

        public void MyConsoleWriteOut(Vector3 v)
        {
            Console.WriteLine("[ " + v[0] + " " + v[1] + " " + v[2] + " ]");
        }
        public void MyConsoleWriteOut(Vector4 v)
        {
            Console.WriteLine("[ " + v[0] + " " + v[1] + " " + v[2] + " " + v[3] + " ]");
        }
        public void MyConsoleWriteOut(string name, Vector3 v)
        {
            Console.WriteLine(name + " = [ " + v[0] + " " + v[1] + " " + v[2] + " ]");
        }
        public void MyConsoleWriteOut(string name, Vector4 v)
        {
            Console.WriteLine(name + " = [ " + v[0] + " " + v[1] + " " + v[2] + " " + v[3] + " ]");
        }

        public void MyConsoleWriteOut(Matrix3 M)
        {
            Console.WriteLine("[ " + M[0, 0] + " " + M[0, 1] + " " + M[0, 2] + " ]");
            Console.WriteLine("[ " + M[1, 0] + " " + M[1, 1] + " " + M[1, 2] + " ]");
            Console.WriteLine("[ " + M[2, 0] + " " + M[2, 1] + " " + M[2, 2] + " ]");
        }
        public void MyConsoleWriteOut(Matrix4 M)
        {
            Console.WriteLine("[ " + M[0, 0] + " " + M[0, 1] + " " + M[0, 2] + " " + M[0, 3] + " ]");
            Console.WriteLine("[ " + M[1, 0] + " " + M[1, 1] + " " + M[1, 2] + " " + M[1, 3] + " ]");
            Console.WriteLine("[ " + M[2, 0] + " " + M[2, 1] + " " + M[2, 2] + " " + M[2, 3] + " ]");
            Console.WriteLine("[ " + M[3, 0] + " " + M[3, 1] + " " + M[3, 2] + " " + M[3, 3] + " ]");
        }
        public void MyConsoleWriteOut(string name, Matrix3 M)
        {
            Console.WriteLine(name + " = ");
            Console.WriteLine("[ " + M[0, 0] + " " + M[0, 1] + " " + M[0, 2] + " ]");
            Console.WriteLine("[ " + M[1, 0] + " " + M[1, 1] + " " + M[1, 2] + " ]");
            Console.WriteLine("[ " + M[2, 0] + " " + M[2, 1] + " " + M[2, 2] + " ]");
        }
        public void MyConsoleWriteOut(string name, Matrix4 M)
        {
            Console.WriteLine(name + " = ");
            Console.WriteLine("[ " + M[0, 0] + " " + M[0, 1] + " " + M[0, 2] + " " + M[0, 3] + " ]");
            Console.WriteLine("[ " + M[1, 0] + " " + M[1, 1] + " " + M[1, 2] + " " + M[1, 3] + " ]");
            Console.WriteLine("[ " + M[2, 0] + " " + M[2, 1] + " " + M[2, 2] + " " + M[2, 3] + " ]");
            Console.WriteLine("[ " + M[3, 0] + " " + M[3, 1] + " " + M[3, 2] + " " + M[3, 3] + " ]");
        }

        public void BilinearInterp(int lod_i, int lod_j, int i, int j, ref Vector4[,] Vstup, out double outX, out double outY)
        {
            float i_dir_param = (float)lod_i / (LOD + 1);
            float j_dir_param = (float)lod_j / (LOD + 1);
            Vector4 V1 = (1 - i_dir_param) * Vstup[i, j] + i_dir_param * Vstup[i + 1, j];
            Vector4 V2 = (1 - i_dir_param) * Vstup[i, j + 1] + i_dir_param * Vstup[i + 1, j + 1];
            Vector4 V = (1 - j_dir_param) * V1 + j_dir_param * V2;
            outX = V.X;
            outY = V.Y;
        }

        public double SquaredEuclidianDist2D(Vector4 V1, Vector4 V2)
        {
            return SquaredEuclidianDist2D(V1.X, V1.Y, V2.X, V2.Y);
        }

        public double SquaredEuclidianDist2D(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return (dx * dx + dy * dy); // *(1/40000);
        }
    }
}
