using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomovaPracaLB
{
    public class TerrainParabHyperb : TerrainData
    {
        public TerrainParabHyperb(int k)
        {
            int size = 2 * k + 1;
            if(k==0)
            {
                 DataPointsAll = new Vector4[1,1];
                DataPointsAll[0, 0] = new Vector4(0, 0, 0, 1);
            }
            else
            {
                DataPointsAll = new Vector4[k+1, k+1];
                float a = (float)3, b = (float)3;
                float x = -(float)k/2, y = -(float)k/2, z = 0;
                for (int i = 0; i < k+1; i++)
                {
                    for (int j = 0; j < k+1; j++)
                    {
                        //z = y * y / (b * b) - x * x / (a * a);
                        z = (float)(Math.Sin(x / 3) * Math.Sin(x / 3) * Math.Cos(y) + Math.Sin(2 * y));
                        DataPointsAll[i, j] = new Vector4(x, y, z, 1.0f);
                        y += 1.0f;
                    }
                    x += 1.0f;
                    y = -(float)k / 2;
                }
            }

            Initialize();
        }
        
    }
}