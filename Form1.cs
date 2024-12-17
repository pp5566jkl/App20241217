using System.Drawing.Imaging;
namespace App20241217
{
    public partial class Form1 : Form
    {

        private Point leftPoint = Point.Empty;   // ���Ϥǰt�I
        private Point rightPoint = Point.Empty;  // �k�Ϥǰt�I
        private const double FocalLength = 12.07; // �J�Z (mm)
        private const double PixelSize = 0.0033450704225352; // �C�����j�p (mm)
        private Bitmap templateImage; // �ҪO�Ϥ�
        private bool isLeftMatched = false, isRightMatched = false;

        public Form1()
        {
            InitializeComponent();



            // ���J�ҪO�Ϥ�
            templateImage = new Bitmap(@"C:\�ҪO��.png");
        }



        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Filter = "�Ϲ����(JPeg, Gif, Bmp, etc.)|.jpg;*jpeg;*.gif;*.bmp;*.tif;*.tiff;*.png|�Ҧ����(*.*)|*.*";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    Bitmap MyBitmap = new Bitmap(openFileDialog1.FileName);
                    this.pictureBox1.Image = MyBitmap;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "�T�����");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Filter = "�Ϲ����(JPeg, Gif, Bmp, etc.)|.jpg;*jpeg;*.gif;*.bmp;*.tif;*.tiff;*.png|�Ҧ����(*.*)|*.*";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    Bitmap MyBitmap = new Bitmap(openFileDialog1.FileName);
                    this.pictureBox2.Image = MyBitmap;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "�T�����");
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            try
            {
                // �ˬd�O�_�����b���檺�ҪO�ǰt
                if (button3.Enabled == false)
                {
                    MessageBox.Show("�ҪO�ǰt���b�i�椤�A�еy��A�աC", "����");
                    return;
                }

                if (pictureBox1.Image != null && pictureBox2.Image != null)
                {
                    Bitmap targetImage1 = new Bitmap(pictureBox1.Image);
                    Bitmap targetImage2 = new Bitmap(pictureBox2.Image);

                    // �T�O�ҪO�Ϥ��w���J
                    if (templateImage == null)
                    {
                        MessageBox.Show("�ҪO�Ϥ������J�A���ˬd�ҪO�ɮ׬O�_�s�b�C", "���~");
                        return;
                    }

                    // �T�Ϋ��s�A�קK�����I��
                    button3.Enabled = false;
                    label1.Text = "�ҪO�ǰt��...";
                    label2.Text = "�ҪO�ǰt��...";

                    // �ϥβ��B�覡����ҪO�ǰt
                    (Point leftPointResult, Point rightPointResult) = await Task.Run(() =>
                    {
                        Point left = TemplateMatch(targetImage1, templateImage);
                        Point right = TemplateMatch(targetImage2, templateImage);
                        return (left, right);
                    });

                    // ��s UI ��ܤǰt���G
                    leftPoint = leftPointResult;
                    rightPoint = rightPointResult;
                    label1.Text = $"���Ϭ��I: ({leftPoint.X}, {leftPoint.Y})";
                    label2.Text = $"�k�Ϭ��I: ({rightPoint.X}, {rightPoint.Y})";

                    // ø�s���I��m�b�Ϥ��W
                    using (Graphics g1 = Graphics.FromImage(targetImage1))
                    {
                        g1.DrawEllipse(Pens.Red, leftPoint.X - 5, leftPoint.Y - 5, 10, 10);
                    }
                    using (Graphics g2 = Graphics.FromImage(targetImage2))
                    {
                        g2.DrawEllipse(Pens.Red, rightPoint.X - 5, rightPoint.Y - 5, 10, 10);
                    }

                    pictureBox1.Image = targetImage1;
                    pictureBox2.Image = targetImage2;

                    // �ǰt��������
                    MessageBox.Show($"�ǰt�����I\n���Ϭ��I�y��: ({leftPoint.X}, {leftPoint.Y})\n�k�Ϭ��I�y��: ({rightPoint.X}, {rightPoint.Y})", "�ǰt���G");
                }
                else
                {
                    MessageBox.Show("�Х����J��i�v���C", "���~");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "���~");
            }
            finally
            {
                // �ҥΫ��s
                button3.Enabled = true;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (isLeftMatched && isRightMatched)
                {
                    if (double.TryParse(textBox1.Text, out double baselineCm))
                    {
                        double baselineMm = baselineCm * 10;
                        double disparityPixels = Math.Abs(leftPoint.X - rightPoint.X);
                        double disparityMm = disparityPixels * PixelSize;

                        if (disparityMm == 0)
                        {
                            MessageBox.Show("���t���s�A�L�k�p��`�סC���ˬd���I����O�_���T�C", "���~");
                            return;
                        }

                        double depthMm = (FocalLength * baselineMm) / disparityMm;
                        double depthCm = depthMm / 10;

                        label1.Text = $"�`��: {depthCm:F2} cm";
                    }
                    else
                    {
                        MessageBox.Show("�п�J���Ī��۾���u�Z���C", "���~");
                    }
                }
                else
                {
                    MessageBox.Show("�Х��i��ҪO�ǰt�C", "���~");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�`�׭p��ɵo�Ϳ��~: {ex.Message}", "���~");
            }
        }

        // �ҪO�ǰt��k (SAD �t��k)
        private Point TemplateMatch(Bitmap target, Bitmap template)
        {
            int tWidth = template.Width;
            int tHeight = template.Height;
            int bestX = 0, bestY = 0;
            double minSAD = double.MaxValue;
            int step = 2;  // �C���B�i2�ӹ����I�ӥ[�t�ǰt

            // �p��Ϲ����X�A�d��
            for (int x = 0; x <= target.Width - tWidth; x += step)
            {
                for (int y = 0; y <= target.Height - tHeight; y += step)
                {
                    double sad = CalculateSAD(target, template, x, y);
                    if (sad < minSAD)
                    {
                        minSAD = sad;
                        bestX = x;
                        bestY = y;
                    }
                }
            }

            return new Point(bestX + tWidth / 2, bestY + tHeight / 2); // ��^���߮y��
        }

        // �p�� SAD (����t�ȩM)
        private double CalculateSAD(Bitmap target, Bitmap template, int startX, int startY)
        {
            double sad = 0;

            // �T�O�ҪO���|�W�X�ؼйϹ����
            if (startX < 0 || startY < 0 || startX + template.Width > target.Width || startY + template.Height > target.Height)
            {
                throw new ArgumentOutOfRangeException("�ҪO�W�X�F�ؼйϹ������");
            }

            // �M���ҪO�Ϲ�����
            for (int x = 0; x < template.Width; x++)
            {
                for (int y = 0; y < template.Height; y++)
                {
                    try
                    {
                        // ���o�ؼйϹ��M�ҪO�Ϲ�������
                        Color targetPixel = target.GetPixel(startX + x, startY + y);
                        Color templatePixel = template.GetPixel(x, y);

                        // �p�� SAD�]����t�ȩM�^
                        sad += Math.Abs(targetPixel.R - templatePixel.R) +
                               Math.Abs(targetPixel.G - templatePixel.G) +
                               Math.Abs(targetPixel.B - templatePixel.B);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"���~�B�z����: {ex.Message}", "���~");
                        return double.MaxValue; // ��o�Ϳ��~�ɡA��^�̤j�ȡA�קK���~���ǰt
                    }
                }
            }

            return sad;
        }

    }
}