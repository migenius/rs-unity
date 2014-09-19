using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace com.migenius.rs4.math
{
    public class Vector3D
    {
        private double[] v = new double[4];

        public Vector3D()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public Vector3D(double x)
        {
            X = x;
            Y = 0;
            Z = 0;
        }

        public Vector3D(double x, double y)
        {
            X = x;
            Y = y;
            Z = 0;
        }

        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3D(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3D clone()
        {
            return new Vector3D(X, Y, Z);
        }
		
        public override string ToString()
        {
            return (X + " " + Y + " " + Z);
        }

        public void SetVector(Vector3D vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
            W = vector.W;
        }

        public void Transform(Matrix3D matrix)
        {
            Vector3D vector = this.clone();
            X = vector.X * matrix.XX + vector.Y * matrix.YX + vector.Z * matrix.ZX + vector.W * matrix.WX;
            Y = vector.Y * matrix.XY + vector.Y * matrix.YY + vector.Z * matrix.ZY + vector.W * matrix.WY;
            Z = vector.Z * matrix.XZ + vector.Y * matrix.YZ + vector.Z * matrix.ZZ + vector.W * matrix.WZ;
            W = vector.Z * matrix.XW + vector.Y * matrix.YW + vector.Z * matrix.ZW + vector.W * matrix.WW;
        }

        public Vector3D TransformConstant(Matrix3D matrix, Vector3D vector_out)
        {
            vector_out.X = X * matrix.XX + Y * matrix.YX + Z * matrix.ZX + W * matrix.WX;
            vector_out.Y = Y * matrix.XY + Y * matrix.YY + Z * matrix.ZY + W * matrix.WY;
            vector_out.Z = Z * matrix.XZ + Y * matrix.YZ + Z * matrix.ZZ + W * matrix.WZ;
            vector_out.W = Z * matrix.XW + Y * matrix.YW + Z * matrix.ZW + W * matrix.WW;
            return vector_out;
        }

        public Vector3D TransformTranspose(Matrix3D matrix)
        {
            Vector3D vector = this.clone();
            X = vector.X * matrix.XX + vector.Y * matrix.XY + vector.Z * matrix.XZ + vector.W * matrix.XW;
            Y = vector.Y * matrix.YZ + vector.Y * matrix.YY + vector.Z * matrix.YZ + vector.W * matrix.YW;
            Z = vector.Z * matrix.ZX + vector.Y * matrix.ZY + vector.Z * matrix.ZZ + vector.W * matrix.ZW;
            W = vector.Z * matrix.WZ + vector.Y * matrix.WY + vector.Z * matrix.WZ + vector.W * matrix.WW;
            return this;
        }

        public Vector3D TransformTransposeConst(Matrix3D matrix, Vector3D vector_out)
        {
            vector_out.X = X * matrix.XX + Y * matrix.XY + Z * matrix.XZ + W * matrix.XW;
            vector_out.Y = Y * matrix.YX + Y * matrix.YY + Z * matrix.YZ + W * matrix.YW;
            vector_out.Z = Z * matrix.ZX + Y * matrix.ZY + Z * matrix.ZZ + W * matrix.ZW;
            vector_out.W = Z * matrix.ZW + Y * matrix.WY + Z * matrix.WZ + W * matrix.WW;
            return vector_out;
        }

        public Vector3D Rotate(Matrix3D matrix)
        {
            Vector3D vector = this.clone();
            X = vector.X * matrix.XX + vector.Y * matrix.YX + vector.Z * matrix.ZX + vector.W * matrix.WX;
            Y = vector.Y * matrix.XY + vector.Y * matrix.YY + vector.Z * matrix.ZY + vector.W * matrix.WY;
            Z = vector.Z * matrix.XZ + vector.Y * matrix.YZ + vector.Z * matrix.ZZ + vector.W * matrix.WZ;
            W = 1;
            return this;
        }

        public Vector3D RotateTranspose(Matrix3D matrix)
        {
            Vector3D vector = this.clone();
            X = vector.X * matrix.XX + vector.Y * matrix.XY + vector.Z * matrix.XZ;
            Y = vector.Y * matrix.YX + vector.Y * matrix.YY + vector.Z * matrix.YZ;
            Z = vector.Z * matrix.ZX + vector.Y * matrix.ZY + vector.Z * matrix.ZZ;
            W = 1;
            return this;
        }

        public double Dot(Vector3D vector)
        {
            return X * vector.X + Y * vector.Y + Z * vector.Z;
        }

        public Vector3D Cross(Vector3D vector)
        {
            Vector3D crossProduct = new Vector3D();
            crossProduct.X = Y * vector.Z - Z * vector.Y;
            crossProduct.Y = Z * vector.X - X * vector.Z;
            crossProduct.Z = X * vector.Y - Y * vector.X;
            return crossProduct;
        }

        public double Length()
        {
            double d = Dot(this);
            try
            {
                return Math.Sqrt(d);
            }
            catch
            {
                return 0;
            }
        }

        public double DistanceTo(Vector3D vector)
        {
            double x = vector.X - X;
            double y = vector.Y - Y;
            double z = vector.Z - Z;
            return Math.Sqrt(x * x + y * y + z * z);
        }

        public Vector3D Normalize()
        {
            double length = this.Length();
            if (length > 0)
            {
                X = X / length;
                Y = Y / length;
                Z = Z / length;
            }
            return this;
        }

        public Vector3D Scale(double scaleFactor)
        {
            X = X * scaleFactor;
            Y = Y * scaleFactor;
            Z = Z * scaleFactor;
            return this;
        }

        public Vector3D Add(Vector3D vector)
        {
            X = X + vector.X;
            Y = Y + vector.Y;
            Z = Z + vector.Z;
            return this;
        }

        public Vector3D Sub(Vector3D vector)
        {
            X = X - vector.X;
            Y = Y - vector.Y;
            Z = Z - vector.Z;
            return this;
        }

        public bool IsNotColinear(Vector3D vector)
        {
            //Vector3D cross = Cross(vector);
            if (vector.X < 0 && vector.Y < 0 && vector.Z < 0)
                return false;
            else
                return true;
        }

        public Hashtable GetVectorForRS()
        {
            return new Hashtable() {
                {"x", X}, {"y", Y}, {"z", Z}, {"w", W}
            };
        }
        
        public double X
        {
            get
            {
                return v[0];
            }
            set {
                v[0] = value;
            }
        }

        public double Y
        {
            get
            {
                return v[1];
            }
            set
            {
                v[1] = value;
            }
        }

        public double Z
        {
            get
            {
                return v[2];
            }
            set
            {
                v[2] = value;
            }
        }

        public double W
        {
            get
            {
                return v[3];
            }
            set
            {
                v[3] = value;
            }
        }
    }

    public class Matrix3D
    {
        private double[,] m = new double[4,4];
        
        public double XX
        {
            get
            {
                return m[0, 0];
            }
            set
            {
                m[0, 0] = value;
            }
        }

        public double XY
        {
            get
            {
                return m[0, 1];
            }
            set
            {
                m[0, 1] = value;
            }
        }

        public double XZ
        {
            get
            {
                return m[0, 2];
            }
            set
            {
                m[0, 2] = value;
            }
        }

        public double XW
        {
            get
            {
                return m[0, 3];
            }
            set
            {
                m[0, 3] = value;
            }
        }

        public double YX
        {
            get
            {
                return m[1, 0];
            }
            set
            {
                m[1, 0] = value;
            }
        }

        public double YY
        {
            get
            {
                return m[1, 1];
            }
            set
            {
                m[1, 1] = value;
            }
        }

        public double YZ
        {
            get
            {
                return m[1, 2];
            }
            set
            {
                m[1, 2] = value;
            }
        }

        public double YW
        {
            get
            {
                return m[1, 3];
            }
            set
            {
                m[1, 3] = value;
            }
        }

        public double ZX
        {
            get
            {
                return m[2, 0];
            }
            set
            {
                m[2, 0] = value;
            }
        }

        public double ZY
        {
            get
            {
                return m[2, 1];
            }
            set
            {
                m[2, 1] = value;
            }
        }

        public double ZZ
        {
            get
            {
                return m[2, 2];
            }
            set
            {
                m[2, 2] = value;
            }
        }

        public double ZW
        {
            get
            {
                return m[2, 3];
            }
            set
            {
                m[2, 3] = value;
            }
        }

        public double WX
        {
            get
            {
                return m[3, 0];
            }
            set
            {
                m[3, 0] = value;
            }
        }

        public double WY
        {
            get
            {
                return m[3, 1];
            }
            set
            {
                m[3, 1] = value;
            }
        }

        public double WZ
        {
            get
            {
                return m[3, 2];
            }
            set
            {
                m[3, 2] = value;
            }
        }

        public double WW
        {
            get
            {
                return m[3, 3];
            }
            set
            {
                m[3, 3] = value;
            }
        }

        public double[,] raw
        {
            get
            {
                return m;
            }
        }

        public Matrix3D()
        {
            Clear();
            Identity();
        }

        public Matrix3D(Matrix3D matrix)
        {
            SetMatrix(matrix);
        }

        public void SetMatrix(Matrix3D matrix)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    m[i, j] = matrix.raw[i,j];
                }
            }
        }

        public void Identity()
        {
            XX = YY = ZZ = WW = 1;
        }

        public void Clear()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    m[i, j] = 0;
                }
            }
        }

        public void Rotate(Vector3D axis, double angle)
        {
            Identity();
            double cos_angle = Math.Cos(angle);
            double sin_angle = Math.Sin(angle);

            XX = (1 - cos_angle) * axis.X * axis.X + cos_angle;
            XY = (1 - cos_angle) * axis.X * axis.Y + (sin_angle * axis.Y);
            XZ = (1 - cos_angle) * axis.X * axis.Z - (sin_angle * axis.Z);

            YX = (1 - cos_angle) * axis.X * axis.Y - (sin_angle * axis.Y);
            YY = (1 - cos_angle) * axis.Y * axis.Y + cos_angle;
            YZ = (1 - cos_angle) * axis.Y * axis.Z + (sin_angle * axis.X);

            ZX = (1 - cos_angle) * axis.X * axis.Z + (sin_angle * axis.Y);
            ZY = (1 - cos_angle) * axis.Y * axis.Z - (sin_angle * axis.X);
            ZZ = (1 - cos_angle) * axis.Z * axis.Z + cos_angle;
        }

        public Matrix3D Clone()
        {
            return new Matrix3D(this);
        }

        public Matrix3D Multiply(Matrix3D matrix)
        {
            Matrix3D mat = Clone();
            XX = mat.XX * matrix.XX + mat.XY * matrix.YX + mat.XZ * matrix.ZX + mat.XW * matrix.ZX;
            XY = mat.XX * matrix.XY + mat.XY * matrix.YY + mat.XZ * matrix.ZY + mat.XW * matrix.ZY;
            XZ = mat.XX * matrix.XZ + mat.XY * matrix.YZ + mat.XZ * matrix.ZZ + mat.XW * matrix.ZZ;
            XW = mat.XX * matrix.XW + mat.XY * matrix.YW + mat.XZ * matrix.ZW + mat.XW * matrix.ZW;
            YX = mat.YX * matrix.XX + mat.YY * matrix.YX + mat.YZ * matrix.ZX + mat.YW * matrix.ZX;
            YY = mat.YX * matrix.XY + mat.YY * matrix.YY + mat.YZ * matrix.ZY + mat.YW * matrix.ZY;
            YZ = mat.YX * matrix.XZ + mat.YY * matrix.YZ + mat.YZ * matrix.ZZ + mat.YW * matrix.ZZ;
            YW = mat.YX * matrix.XW + mat.YY * matrix.YW + mat.YZ * matrix.ZW + mat.YW * matrix.ZW;
            ZX = mat.ZX * matrix.XX + mat.ZY * matrix.YX + mat.ZZ * matrix.ZX + mat.ZW * matrix.ZX;
            ZY = mat.ZX * matrix.XY + mat.ZY * matrix.YY + mat.ZZ * matrix.ZY + mat.ZW * matrix.ZY;
            ZZ = mat.ZX * matrix.XZ + mat.ZY * matrix.YZ + mat.ZZ * matrix.ZZ + mat.ZW * matrix.ZZ;
            ZW = mat.ZX * matrix.XW + mat.ZY * matrix.YW + mat.ZZ * matrix.ZW + mat.ZW * matrix.ZW;
            WX = mat.WX * matrix.XX + mat.WY * matrix.YX + mat.WZ * matrix.ZX + mat.WW * matrix.ZX;
            WY = mat.WX * matrix.XY + mat.WY * matrix.YY + mat.WZ * matrix.ZY + mat.WW * matrix.ZY;
            WZ = mat.WX * matrix.XZ + mat.WY * matrix.YZ + mat.WZ * matrix.ZZ + mat.WW * matrix.ZZ;
            WW = mat.WX * matrix.XW + mat.WY * matrix.YW + mat.WZ * matrix.ZW + mat.WW * matrix.ZW;
            return this;
        }
		
		public Matrix3D Scale(Vector3D scale)
		{
			XX /= scale.X;
			YX /= scale.X;
			ZX /= scale.X;
			WX /= scale.X;
			XY /= scale.Y;
			YY/= scale.Y;
			ZY /= scale.Y;
			WY/= scale.Y;
			XZ /= scale.Z;
			YZ /= scale.Z;
			ZZ /= scale.Z;
			WZ /= scale.Z;
			return this;
		}

        public Matrix3D Transpose()
        {
            Matrix3D matrix = Clone();
            Clear();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    raw[i, j] = matrix.raw[j, i];
                }
            }
            return this;
        }

        public double Determinant()
        {
            double determinant = 0;
            for (var i = 0; i < 4; i++)
            {
                double sign = ((i & 1) == 0) ? 1 : -1;
                determinant = determinant + (sign * raw[0, i] * DeterminantRc(0, i));
            }
            return determinant;
        }

        public double DeterminantRc(int pos_one, int pos_two)
        {
            double[] output = new double[9];
            for (int i = 0; i < 4; i++)
            {
                if (i != pos_one)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        output[(i * 3) + j] = raw[i, j];
                    }
                }
            }
            return output[0] * ((output[4] * output[8]) - (output[7] * output[5])) - output[1] * ((output[3] * output[8]) - (output[6] * output[5])) + output[2] * ((output[3] * output[7]) - (output[6] * output[4]));
        }

        public Matrix3D Invert()
        {
            double determinant = Determinant();
            Matrix3D matrix = Clone();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    double sign = (((i + j) & 1) == 0) ? 1 : -1;
                    raw[i, j] = sign * matrix.DeterminantRc(i, j) / determinant;
                }
            }
            return this;
        }

        public Matrix3D SetTranslation(double x, double y, double z)
        {
            raw[3, 0] = x;
            raw[3, 1] = y;
            raw[3, 2] = z;
            return this;
        }

        public string GetMatrixForMI(string seperator)
        {
			if(seperator == "" || seperator == null)
				 seperator = "\n";
            return XX + " " + XY + " " + XZ + " " + XW + seperator + YX + " " + YY + " " + YZ + " " + YW + seperator + ZX + " " + ZY + " " + ZZ + " " + ZW + seperator + WX + " " + WY + " " + WZ + " " + WW + "\n";
        }
		
        public Hashtable GetMatrixForRS()
        {
            return new Hashtable() {
                {"xx", XX}, {"xy", XY}, {"xz", XZ}, {"xw", XW},
                {"yx", YX}, {"yy", YY}, {"yz", YZ}, {"yw", YW},
                {"zx", ZX}, {"zy", ZY}, {"zz", ZZ}, {"zw", ZW},
                {"wx", WX}, {"wy", WY}, {"wz", WZ}, {"ww", WW}
            };
        }
		
        public override string ToString()
        {
			return XX + " " + XY + " " + XZ + " " + XW + "\n" + YX + " " + YY + " " + YZ + " " + YW + "\n" + ZX + " " + ZY + " " + ZZ + " " + ZW + "\n" + WX + " " + WY + " " + WZ + " " + WW + "\n";
        }

        public bool Compare(Matrix3D rhs)
        {
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    double d = Math.Abs(rhs.raw[x, y] - raw[x, y]);
                    if (d > 0.0001)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    public class Transform3D
    {
        public Vector3D Z_DIR = new Vector3D(0, 1, 0);
        public Vector3D Z_UP = new Vector3D(0, 0, 1);
        public Vector3D Z_RIGHT = new Vector3D(1, 0, 0);
        public Vector3D Y_DIR = new Vector3D(0, 0, -1);
        public Vector3D Y_UP = new Vector3D(0, 1, 0);
        public Vector3D Y_RIGHT = new Vector3D(1, 0, 0);
        public const int COORD_Y_UP = 0;
        public const int COORD_Z_UP = 1;
        public Matrix3D world_to_object;
        private int coord_system;
        private Vector3D ref_dir;
        private Vector3D ref_up;
        private Vector3D ref_right;
        public Vector3D location;
        public Vector3D direction;
        public Vector3D up;
        public Vector3D right;
        public Vector3D target;

        public Transform3D()
        {
            world_to_object = new Matrix3D();
            coord_system = COORD_Z_UP;
            ref_dir = Z_DIR;
            ref_up = Z_UP;
            ref_right = Z_RIGHT;

            location = new Vector3D();
            direction = ref_dir.clone();
            up = ref_up.clone();
            right = ref_right.clone();
            target = new Vector3D();
        }

        public Transform3D Clone()
        {
            Transform3D c = new Transform3D();
            c.world_to_object = world_to_object.Clone();
            c.coord_system = coord_system;
            c.ref_dir = ref_dir.clone();
            c.ref_up = ref_up.clone();
            c.ref_right = ref_right.clone();
            c.location = location.clone();
            c.direction = direction.clone();
            c.up = up.clone();
            c.target = target.clone();
            //TODO: Derive Vectors
            return c;
        }

        public void UpdateRefDirs()
        {
            if (coord_system == COORD_Y_UP)
            {
                ref_dir = Y_DIR;
                ref_right = Y_RIGHT;
                ref_up = Y_UP;
            }
            else
            {
                ref_dir = Z_DIR;
                ref_up = Z_UP;
                ref_right = Z_RIGHT;
            }
        }

        public void DeriveVectors() {
            Matrix3D m = world_to_object.Clone();
            m.Invert();

            double d = target.Sub(location).Length();
            if (d <= 0)
                d = 10;

            location.SetVector(new Vector3D(0, 0, 0));
            location.TransformTranspose(m);
            
            direction.SetVector(new Vector3D(0, 0, -1));
            direction.RotateTranspose(m);
            direction.Normalize();

            up.SetVector(new Vector3D(0, 1, 0));
            up.RotateTranspose(m);
            up.Normalize();

            right.SetVector(new Vector3D(1, 0, 0));
            right.RotateTranspose(m);
            right.Normalize();

            target = location.clone().Add(direction.clone().Scale(d));
        }

        public void DeriveMatrix()
        {
            world_to_object.Identity();
            world_to_object.XX = right.X;
            world_to_object.YX = right.Y;
            world_to_object.ZX = right.Z;

            world_to_object.XY = up.X;
            world_to_object.YY = up.Y;
            world_to_object.ZY = up.Z;

            world_to_object.XZ = -direction.X;
            world_to_object.YZ = -direction.Y;
            world_to_object.ZZ = -direction.Z;

            Vector3D v = new Vector3D();
            for (int i = 0; i < 3; i++)
            {
                v.SetVector(new Vector3D(world_to_object.raw[0, i], world_to_object.raw[1, i], world_to_object.raw[2, i]));
                world_to_object.raw[3, i] = -1 * location.Dot(v);
            }
        }

        public void SetLookAt(Vector3D position, Vector3D direction, Vector3D up)
        {
            location = position;
            if (direction != null && up != null && direction.IsNotColinear(up))
            {
                direction.Normalize();
                up.Normalize();
                this.direction = direction.clone();
                this.up = up.clone();
                CalcRU();
                DeriveMatrix();
            }
        }

        public void CalcRU()
        {
            right.SetVector(up.Cross(direction));
            right.Scale(-1);
            right.Normalize();
            up.SetVector(right.Cross(direction).Normalize());
        }

        public void SetLookAtPoint(Vector3D position, Vector3D target, Vector3D up)
        {
            Vector3D direction = target.clone().Sub(position);
            if (direction != null && up != null && direction.IsNotColinear(up))
            {
                this.location = position;
                this.target = target;
                direction.Normalize();
                up.Normalize();
                this.direction = direction.clone();
                this.up = up.clone();
                CalcRU();
                DeriveMatrix();
            }
        }

        public void pan(double x, double y) {
            Vector3D dx = right.clone().Scale(x);
            Vector3D dy = up.clone().Scale(y);

            location.Add(dx);
            location.Add(dy);

            target.Add(dx);
            target.Add(dy);
        }

        public void LookAtTargetPoint()
        {
            Vector3D lookDir = target.clone();
            lookDir.Sub(location).Normalize();

            if (lookDir.IsNotColinear(ref_up))
                up.SetVector(ref_up);
            else
                up.SetVector(ref_right);
            SetLookAt(location, lookDir, up);
        }

        public void Move(Vector3D distance)
        {
            location.Add(distance);
            LookAtTargetPoint();
        }

        public void Move(Vector3D distance, bool moveTargetPoint)
        {
            location.Add(distance);
            target.Add(distance);
            LookAtTargetPoint();
        }

        public void MoveTo(Vector3D loc)
        {
            location.SetVector(location);
            LookAtTargetPoint();
        }

        public void MoveTo(Vector3D loc, bool shiftTarget) {
            Vector3D previousLocation = location;
            location.SetVector(loc);
            target.Add(loc.Sub(previousLocation));
        }
		
        public override string ToString()
        {
			return ("Right: " + world_to_object.XX + " " + world_to_object.YX + " " + world_to_object.ZX + "\n" +
						"Up: " + world_to_object.XY + " " + world_to_object.YY + " " + world_to_object.ZY + "\n" +
						"Direction: " + -world_to_object.XZ + " " + -world_to_object.YZ + " " + -world_to_object.ZZ + "\n");;
        }
    }
}
