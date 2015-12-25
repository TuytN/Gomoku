using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using Quobject.SocketIoClientDotNet.Client;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Windows.Threading;

namespace DACK
{


    public enum Player { Out = -1, None = 0, Human = 1, Machine = 2 }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public struct Node
    {
        public int Row;
        public int Column;
    }

    public class EValueboard
    {
        // ************ VARIABLE *********************************
        public int Width, Height;
        public int[,] board;

        // ************ CONSTRUCTOR ******************************
        public EValueboard()
        {
            Width = 12;
            Height = 12;
            board = new int[Height + 2, Width + 2];

            Resetboard();
        }
        //Download source code tai Sharecode.vn
        // ************ ADDING FUNCTION **************************
        public void Resetboard()
        {
            for (int r = 0; r < Height + 2; r++)
                for (int c = 0; c < Width + 2; c++)
                    board[r, c] = 0;
        }
        //Download source code tai Sharecode.vn
        public Node GetMaxNode()
        {
            int r, c, MaxValue = 0;
            Node n = new Node();

            for (r = 1; r <= Height; r++)
                for (c = 1; c <= Width; c++)
                    if (board[r, c] > MaxValue)
                    {
                        n.Row = r; n.Column = c;
                        MaxValue = board[r, c];
                    }

            return n;
        }
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            fill();

        }

        private readonly BackgroundWorker worker = new BackgroundWorker();

        public EValueboard Eboard = new EValueboard();

        Player[,] board = new Player[14, 14];
        int col = -1, row = -1;
        Player turn = Player.Human;
        //1 if player 1 (x)
        //2 if player 2 (o)
        //3 if comp (o)
        int Width = 12;
        int Height = 12;
        int playmode = 1;
        //1 if play offline 2 players;
        //2 if play offline with comp
        //3 if play online
        //4 if play online auto

        // Phong thu chat, tan cong nhanh.
        public int[] TScore = new int[5] { 0, 1, 9, 85, 769 };
        public int[] KScore = new int[5] { 0, 2, 28, 256, 2308 };

        int count;
        Node node = new Node();
        Player End = Player.None;

        Socket socket;
        //int playfirst = 0;//-1: not accept, 0: first, 1: second

        // Luong gia cho ban co - kinh nghiem cua may.
        private void EvalueGomokuboard(Player player)
        {
            int rw, cl, i;
            int cntHuman, cntMachine;
            //Download source code tai Sharecode.vn
            // Luong gia cho hang.
            for (rw = 1; rw <= Height; rw++)
                for (cl = 1; cl <= Width - 4; cl++)
                {
                    cntHuman = 0; cntMachine = 0;
                    for (i = 0; i < 5; i++)
                    {
                        if (board[rw, cl + i] == Player.Human) cntHuman++;
                        if (board[rw, cl + i] == Player.Machine) cntMachine++;
                    }
                    // Luong gia...
                    if (cntHuman * cntMachine == 0 && cntHuman != cntMachine)
                        for (i = 0; i < 5; i++)
                            if (board[rw, cl + i] == Player.None)
                            {
                                if (cntMachine == 0)
                                {
                                    if (player == Player.Machine) Eboard.board[rw, cl + i] += TScore[cntHuman];
                                    else Eboard.board[rw, cl + i] += KScore[cntHuman];
                                    // Truong hop bi chan 2 dau.
                                    if (board[rw, cl - 1] == Player.Machine && board[rw, cl + 5] == Player.Machine)
                                        Eboard.board[rw, cl + i] = 0;
                                }
                                if (cntHuman == 0)
                                {
                                    if (player == Player.Human) Eboard.board[rw, cl + i] += TScore[cntMachine];
                                    else Eboard.board[rw, cl + i] += KScore[cntMachine];
                                    // Truong hop bi chan 2 dau.
                                    if (board[rw, cl - 1] == Player.Human && board[rw, cl + 5] == Player.Human)
                                        Eboard.board[rw, cl + i] = 0;
                                }
                                if ((cntHuman == 4 || cntMachine == 4)
                                    && (board[rw, cl + i - 1] == Player.None || board[rw, cl + i + 1] == Player.None))
                                    Eboard.board[rw, cl + i] *= 2;
                            }
                }
            //Download source code tai Sharecode.vn
            // Luong gia cho cot.
            for (cl = 1; cl <= Width; cl++)
                for (rw = 1; rw <= Height - 4; rw++)
                {
                    cntHuman = 0; cntMachine = 0;
                    for (i = 0; i < 5; i++)
                    {
                        if (board[rw + i, cl] == Player.Human) cntHuman++;
                        if (board[rw + i, cl] == Player.Machine) cntMachine++;
                    }
                    // Luong gia...
                    if (cntHuman * cntMachine == 0 && cntMachine != cntHuman)
                        for (i = 0; i < 5; i++)
                            if (board[rw + i, cl] == Player.None)
                            {
                                if (cntMachine == 0)
                                {
                                    if (player == Player.Machine) Eboard.board[rw + i, cl] += TScore[cntHuman];
                                    else Eboard.board[rw + i, cl] += KScore[cntHuman];
                                    // Truong hop bi chan 2 dau.
                                    if (board[rw - 1, cl] == Player.Machine && board[rw + 5, cl] == Player.Machine)
                                        Eboard.board[rw + i, cl] = 0;
                                }
                                if (cntHuman == 0)
                                {
                                    if (player == Player.Human) Eboard.board[rw + i, cl] += TScore[cntMachine];
                                    else Eboard.board[rw + i, cl] += KScore[cntMachine];
                                    // Truong hop bi chan 2 dau.
                                    if (board[rw - 1, cl] == Player.Human && board[rw + 5, cl] == Player.Human)
                                        Eboard.board[rw + i, cl] = 0;

                                }
                                if ((cntHuman == 4 || cntMachine == 4)
                                    && (board[rw + i - 1, cl] == Player.None || board[rw + i + 1, cl] == Player.None))
                                    Eboard.board[rw + i, cl] *= 2;
                            }
                }


            // Luong gia cho duong cheo xuong.
            for (rw = 1; rw <= Height - 4; rw++)
                for (cl = 1; cl <= Width - 4; cl++)
                {
                    cntHuman = 0; cntMachine = 0;
                    for (i = 0; i < 5; i++)
                    {
                        if (board[rw + i, cl + i] == Player.Human) cntHuman++;
                        if (board[rw + i, cl + i] == Player.Machine) cntMachine++;
                    }
                    // Luong gia...
                    if (cntHuman * cntMachine == 0 && cntMachine != cntHuman)
                        for (i = 0; i < 5; i++)
                            if (board[rw + i, cl + i] == Player.None)
                            {
                                if (cntMachine == 0)
                                {
                                    if (player == Player.Machine) Eboard.board[rw + i, cl + i] += TScore[cntHuman];
                                    else Eboard.board[rw + i, cl + i] += KScore[cntHuman];
                                    // Truong hop bi chan 2 dau.
                                    if (board[rw - 1, cl - 1] == Player.Machine && board[rw + 5, cl + 5] == Player.Machine)
                                        Eboard.board[rw + i, cl + i] = 0;
                                }
                                if (cntHuman == 0)
                                {
                                    if (player == Player.Human) Eboard.board[rw + i, cl + i] += TScore[cntMachine];
                                    else Eboard.board[rw + i, cl + i] += KScore[cntMachine];
                                    // Truong hop bi chan 2 dau.
                                    if (board[rw - 1, cl - 1] == Player.Human && board[rw + 5, cl + 5] == Player.Human)
                                        Eboard.board[rw + i, cl + i] = 0;
                                }
                                if ((cntHuman == 4 || cntMachine == 4)
                                    && (board[rw + i - 1, cl + i - 1] == Player.None || board[rw + i + 1, cl + i + 1] == Player.None))
                                    Eboard.board[rw + i, cl + i] *= 2;
                            }
                }

            // Luong gia cho duong cheo len.
            for (rw = 5; rw <= Height - 4; rw++)
                for (cl = 1; cl <= Width - 4; cl++)
                {
                    cntMachine = 0; cntHuman = 0;
                    for (i = 0; i < 5; i++)
                    {
                        if (board[rw - i, cl + i] == Player.Human) cntHuman++;
                        if (board[rw - i, cl + i] == Player.Machine) cntMachine++;
                    }
                    // Luong gia...
                    if (cntHuman * cntMachine == 0 && cntHuman != cntMachine)
                        for (i = 0; i < 5; i++)
                            if (board[rw - i, cl + i] == Player.None)
                            {
                                if (cntMachine == 0)
                                {
                                    if (player == Player.Machine) Eboard.board[rw - i, cl + i] += TScore[cntHuman];
                                    else Eboard.board[rw - i, cl + i] += KScore[cntHuman];
                                    // Truong hop bi chan 2 dau.
                                    if (board[rw + 1, cl - 1] == Player.Machine && board[rw - 5, cl + 5] == Player.Machine)
                                        Eboard.board[rw - i, cl + i] = 0;
                                }
                                if (cntHuman == 0)
                                {
                                    if (player == Player.Human) Eboard.board[rw - i, cl + i] += TScore[cntMachine];
                                    else Eboard.board[rw - i, cl + i] += KScore[cntMachine];
                                    // Truong hop bi chan 2 dau.
                                    if (board[rw + 1, cl - 1] == Player.Human && board[rw - 5, cl + 5] == Player.Human)
                                        Eboard.board[rw - i, cl + i] = 0;
                                }
                                if ((cntHuman == 4 || cntMachine == 4)
                                    && (board[rw - i + 1, cl + i - 1] == Player.None || board[rw - i - 1, cl + i + 1] == Player.None))
                                    Eboard.board[rw - i, cl + i] *= 2;
                            }
                }
        }

        // Sinh nuoc di - do thong minh cua may.
        public int Depth = 0;
        static public int MaxDepth = 21;
        static public int MaxBreadth = 8;
        public Node[] WinMoves = new Node[MaxDepth + 1];
        public Node[] MyMoves = new Node[MaxBreadth + 1];
        public Node[] HisMoves = new Node[MaxBreadth + 1];
        public bool Win, Lose;
        // Ham de quy - Sinh nuoc di cho may.

        public void ResetBoard()
        {
            int r, c;
            //Thiet lap lai gia tri bang.
            for (r = 0; r < Height + 2; r++)
                for (c = 0; c < Height + 2; c++)
                {
                    if (r == 0 || c == 0 || r == Height + 1 || c == Width + 1)
                        board[r, c] = Player.Out;
                    else board[r, c] = Player.None;
                }

            turn = Player.Human;//NGUOI CHOI 


        }

        private Player CheckEnd(int rw, int cl)
        {
            bool Human, Machine;
            int r = 1, c = 1;
            int i;

            // Kiem tra tren hang...
            while (c <= Width - 4)
            {
                Human = true; Machine = true;

                for (i = 0; i < 5; i++)
                {
                    if (board[rw, c + i] != Player.Human)
                        Human = false;
                    if (board[rw, c + i] != Player.Machine)
                        Machine = false;
                }

                if (Human && (board[rw, c - 1] != Player.Machine || board[rw, c + 5] != Player.Machine)) return Player.Human;
                if (Machine && (board[rw, c - 1] != Player.Human || board[rw, c + 5] != Player.Human)) return Player.Machine;
                c++;
            }

            // Kiem tra tren cot...
            while (r <= Height - 4)
            {
                Human = true; Machine = true;
                for (i = 0; i < 5; i++)
                {
                    if (board[r + i, cl] != Player.Human)
                        Human = false;
                    if (board[r + i, cl] != Player.Machine)
                        Machine = false;
                }
                if (Human && (board[r - 1, cl] != Player.Machine || board[r + 5, cl] != Player.Machine)) return Player.Human;
                if (Machine && (board[r - 1, cl] != Player.Human || board[r + 5, cl] != Player.Human)) return Player.Machine;
                r++;
            }

            // Kiem tra tren duong cheo xuong.
            r = rw; c = cl;
            // Di chuyen den dau duong cheo xuong.
            while (r > 1 && c > 1) { r--; c--; }
            while (r <= Height - 4 && c <= Width - 4)
            {
                Human = true; Machine = true;
                for (i = 0; i < 5; i++)
                {
                    if (board[r + i, c + i] != Player.Human)
                        Human = false;
                    if (board[r + i, c + i] != Player.Machine)
                        Machine = false;
                }
                if (Human && (board[r - 1, c - 1] != Player.Machine || board[r + 5, c + 5] != Player.Machine)) return Player.Human;
                if (Machine && (board[r - 1, c - 1] != Player.Human || board[r + 5, c + 5] != Player.Human)) return Player.Machine;
                r++; c++;
            }

            // Kiem tra duong cheo len...
            r = rw; c = cl;
            // Di chuyen den dau duong cheo len...
            while (r < Height && c > 1) { r++; c--; }
            while (r >= 5 && c <= Width - 4)
            {
                Human = true; Machine = true;
                for (i = 0; i < 5; i++)
                {
                    if (board[r - i, c + i] != Player.Human)
                        Human = false;
                    if (board[r - i, c + i] != Player.Machine)
                        Machine = false;
                }
                if (Human && (board[r + 1, c - 1] != Player.Machine || board[r - 5, c + 5] != Player.Machine)) return Player.Human;
                if (Machine && (board[r + 1, c - 1] != Player.Human || board[r - 5, c + 5] != Player.Human)) return Player.Machine;
                r--; c++;
            }

            return Player.None;
        }

        public void GenerateMoves()
        {
            if (Depth >= MaxDepth) return;
            Depth++;
            bool lose = false;
            Win = false;

            Node MyNode = new Node();   // Duong di quan ta.
            Node HisNode = new Node();  // Duong di doi thu.
            int count = 0;
            //Download source code tai Sharecode.vn
            // Luong gia cho ma tran.
            EvalueGomokuboard(Player.Machine);

            // Lay MaxBreadth nuoc di tot nhat.
            for (int i = 1; i <= MaxBreadth; i++)
            {
                MyNode = Eboard.GetMaxNode();
                MyMoves[i] = MyNode;
                Eboard.board[MyNode.Row, MyNode.Column] = 0;
            }
            // Lay nuoc di ra khoi danh sach - Danh thu nuoc di.
            count = 0;
            while (count < MaxBreadth)
            {
                count++;
                MyNode = MyMoves[count];
                WinMoves.SetValue(MyNode, Depth);
                board[MyNode.Row, MyNode.Column] = Player.Machine;

                // Tim cac nuoc di toi uu cua doi thu.
                Eboard.Resetboard();
                EvalueGomokuboard(Player.Human);
                for (int i = 1; i <= MaxBreadth; i++)
                {
                    HisNode = Eboard.GetMaxNode();
                    HisMoves[i] = HisNode;
                    Eboard.board[HisNode.Row, HisNode.Column] = 0;
                }

                for (int i = 1; i <= MaxBreadth; i++)
                {
                    HisNode = HisMoves[i];
                    board[HisNode.Row, HisNode.Column] = Player.Human;
                    // Kiem tra ket qua nuoc di.
                    if (CheckEnd(MyNode.Row, MyNode.Column) == Player.Machine)
                        Win = true;
                    if (CheckEnd(HisNode.Row, HisNode.Column) == Player.Human)
                        lose = true;

                    if (lose)
                    {
                        // Loai nuoc di thu.
                        Lose = true;
                        board[HisNode.Row, HisNode.Column] = Player.None;
                        board[MyNode.Row, MyNode.Column] = Player.None;
                        return;
                    }

                    if (Win)
                    {
                        // Loai nuoc di thu.
                        board[HisNode.Row, HisNode.Column] = Player.None;
                        board[MyNode.Row, MyNode.Column] = Player.None;
                        return;
                    }
                    else GenerateMoves(); // tim tiep.
                    // Loai nuoc di thu.
                    board[HisNode.Row, HisNode.Column] = Player.None;
                }

                board[MyNode.Row, MyNode.Column] = Player.None;
            }

        }
        //Download source code tai Sharecode.vn
        // Goi Generator - Tim duong di cho may.
        public void GetGenResult()
        {
            Win = Lose = false;
            // Xoa mang duong di.
            WinMoves = new Node[MaxDepth + 1];
            for (int i = 0; i <= MaxDepth; i++)
                WinMoves[i] = new Node();

            // Xoa stack.
            for (int i = 0; i < MaxBreadth; i++)
                MyMoves[i] = new Node();

            Depth = 0;
            GenerateMoves();
            //if (Win && !Lose)
            //    MessageBox.Show("3"); //Parent.SendMessage(3);
        }

        private void fill()
        {
            int temp_row = 0;
            int temp_col = 0;

            // calc row mouse was over
            foreach (var rowDefinition in LayoutRoot.RowDefinitions)
            {
                foreach (var columnDefinition in LayoutRoot.ColumnDefinitions)
                {
                    if ((temp_row % 2 == 0 && temp_col % 2 == 0) || (temp_row % 2 == 1 && temp_col % 2 == 1))
                    {
                        Border border = new Border { Background = new SolidColorBrush(Colors.LightBlue) };

                        Grid.SetRow(border, temp_row);
                        Grid.SetColumn(border, temp_col);
                        LayoutRoot.Children.Add(border);
                    }

                    if ((temp_row % 2 == 1 && temp_col % 2 == 0) || (temp_row % 2 == 0 && temp_col % 2 == 1))
                    {
                        Border border = new Border { Background = new SolidColorBrush(Colors.LightYellow) };

                        Grid.SetRow(border, temp_row);
                        Grid.SetColumn(border, temp_col);
                        LayoutRoot.Children.Add(border);
                    }

                    temp_col++;
                }
                temp_col = 0;
                temp_row++;
            }

            // calc col mouse was over

        }

        private void OnPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            col = 0;
            row = 0;

            if (e.ClickCount == 1) // for double-click, remove this condition if only want single click
            {
                var point = Mouse.GetPosition(LayoutRoot);

                double accumulatedHeight = 0.0;
                double accumulatedWidth = 0.0;

                // calc row mouse was over
                foreach (var rowDefinition in LayoutRoot.RowDefinitions)
                {
                    accumulatedHeight += rowDefinition.ActualHeight;
                    if (accumulatedHeight >= point.Y)
                        break;
                    row++;
                }

                // calc col mouse was over
                foreach (var columnDefinition in LayoutRoot.ColumnDefinitions)
                {
                    accumulatedWidth += columnDefinition.ActualWidth;
                    if (accumulatedWidth >= point.X)
                        break;
                    col++;
                }

                row++;
                col++;

                Random rand = new Random();
                count = rand.Next(4);

                if (playmode == 2)
                {

                    if (board[row, col] == Player.None)
                    {
                        // nguoi choi di co.
                        if (turn == Player.Human && End == Player.None)
                        {
                            //Parent.SendMessage(0);
                            board[row, col] = turn;
                            turn = Player.Machine;

                            End = CheckEnd(row, col);

                            TextBlock txtBlock1 = new TextBlock();
                            txtBlock1.FontSize = 20;
                            txtBlock1.FontWeight = FontWeights.Bold;
                            txtBlock1.Foreground = new SolidColorBrush(Colors.Green);
                            txtBlock1.VerticalAlignment = VerticalAlignment.Center;
                            txtBlock1.HorizontalAlignment = HorizontalAlignment.Center;
                            txtBlock1.Text = "X";
                            Grid.SetRow(txtBlock1, row - 1);
                            Grid.SetColumn(txtBlock1, col - 1);
                            LayoutRoot.Children.Add(txtBlock1);

                            if (End != Player.None)
                            {
                                MessageBoxResult re = new MessageBoxResult();
                                if (End == Player.Human)
                                    re = MessageBox.Show("you win, wanna play again ? ", "", MessageBoxButton.OKCancel);
                                if (End == Player.Machine)
                                    re = MessageBox.Show("machine win, wanna play again ? ", "", MessageBoxButton.OKCancel);

                                if (re == MessageBoxResult.OK)
                                {
                                    board = new Player[14, 14];
                                    LayoutRoot.Children.Clear();
                                    fill();
                                    cbAutoMode.IsChecked = false;
                                    cbOnlMode.IsChecked = false;
                                    turn = Player.Human;
                                    End = Player.None;
                                }
                                else
                                {
                                    Application.Current.Shutdown();
                                }
                            }
                        }
                        //Download source code tai Sharecode.vn
                        // May di co.
                        if (turn == Player.Machine && End == Player.None)
                        {
                            //worker.DoWork += worker_DoWork;
                            //worker.RunWorkerCompleted += worker_RunWorkerCompleted;
                            //worker.RunWorkerAsync();

                            // Tim nuoc di chien thang.
                            Eboard.Resetboard();
                            GetGenResult();

                            if (Win) // Tim thay.
                            {
                                node = WinMoves[1];
                            }
                            else
                            {
                                Eboard.Resetboard();
                                EvalueGomokuboard(Player.Machine);

                                //Thread machineThread = new Thread(new ThreadStart(InitializationThread));


                                node = Eboard.GetMaxNode();
                                if (!Lose)
                                    for (int i = 0; i < count; i++)
                                    {
                                        Eboard.board[node.Row, node.Column] = 0;
                                        node = Eboard.GetMaxNode();
                                    }
                            }
                            // May di quan.
                            row = node.Row; col = node.Column;
                            board[row, col] = turn;
                            turn = Player.Human;

                            // Kiem tra tran dau ket thuc chua ?
                            End = CheckEnd(row, col);

                            TextBlock txtBlock1 = new TextBlock();
                            txtBlock1.FontSize = 20;
                            txtBlock1.FontWeight = FontWeights.Bold;
                            txtBlock1.Foreground = new SolidColorBrush(Colors.Green);
                            txtBlock1.VerticalAlignment = VerticalAlignment.Center;
                            txtBlock1.HorizontalAlignment = HorizontalAlignment.Center;
                            txtBlock1.Text = "O";
                            Grid.SetRow(txtBlock1, row - 1);
                            Grid.SetColumn(txtBlock1, col - 1);
                            LayoutRoot.Children.Add(txtBlock1);
                            if (End != Player.None)
                            {
                                MessageBoxResult re = new MessageBoxResult();
                                if (End == Player.Human)
                                    re = MessageBox.Show("you win, wanna play again ? ", "", MessageBoxButton.OKCancel);
                                if (End == Player.Machine)
                                    re = MessageBox.Show("machine win, wanna play again ? ", "", MessageBoxButton.OKCancel);

                                if (re == MessageBoxResult.OK)
                                {
                                    board = new Player[14, 14];
                                    LayoutRoot.Children.Clear();
                                    fill();
                                    cbAutoMode.IsChecked = false;
                                    cbOnlMode.IsChecked = false;
                                    turn = Player.Human;
                                    End = Player.None;
                                }
                                else
                                {
                                    Application.Current.Shutdown();
                                }

                            }
                        }

                    }
                }

                else if (playmode == 1)
                {
                    //row--;
                    //col--;
                    if (board[row, col] == Player.None)
                    {
                        TextBlock txtBlock1 = new TextBlock();
                        if (turn == Player.Human)
                            txtBlock1.Text = "X";
                        else if (turn == Player.Machine)
                            txtBlock1.Text = "O";
                        txtBlock1.FontSize = 20;
                        txtBlock1.FontWeight = FontWeights.Bold;
                        txtBlock1.Foreground = new SolidColorBrush(Colors.Green);
                        txtBlock1.VerticalAlignment = VerticalAlignment.Center;
                        txtBlock1.HorizontalAlignment = HorizontalAlignment.Center;
                        Grid.SetRow(txtBlock1, row - 1);
                        Grid.SetColumn(txtBlock1, col - 1);
                        LayoutRoot.Children.Add(txtBlock1);
                        board[row, col] = turn;

                        End = CheckEnd(row, col);
                        if (End != Player.None)
                        {
                            MessageBoxResult re = new MessageBoxResult();
                            if (End == Player.Human)
                                re = MessageBox.Show("player 1 win, wanna play again ? ", "", MessageBoxButton.OKCancel);
                            if (End == Player.Machine)
                                re = MessageBox.Show("player 2 win, wanna play again ? ", "", MessageBoxButton.OKCancel);

                            if (re == MessageBoxResult.OK)
                            {
                                board = new Player[14, 14];
                                LayoutRoot.Children.Clear();
                                fill();
                                cbAutoMode.IsChecked = false;
                                cbOnlMode.IsChecked = false;
                                turn = Player.Human;
                                End = Player.None;
                            }
                            else
                            {
                                Application.Current.Shutdown();
                            }
                        }

                        turn = (turn == Player.Human ? Player.Machine : Player.Human);


                    }
                }

                else if (playmode == 3)
                {
                    socket.Emit("MyStepIs", JObject.FromObject(new { player = 2, row = row - 1, col = col - 1 }));
                }
            }

        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Tim nuoc di chien thang.
            Eboard.Resetboard();
            GetGenResult();

            if (Win) // Tim thay.
            {
                node = WinMoves[1];
            }
            else
            {
                Eboard.Resetboard();
                EvalueGomokuboard(Player.Machine);

                //Thread machineThread = new Thread(new ThreadStart(InitializationThread));


                node = Eboard.GetMaxNode();
                if (!Lose)
                    for (int i = 0; i < count; i++)
                    {
                        Eboard.board[node.Row, node.Column] = 0;
                        node = Eboard.GetMaxNode();
                    }
            }
            // May di quan.
            row = node.Row; col = node.Column;
            board[row, col] = turn;
            turn = Player.Human;

            // Kiem tra tran dau ket thuc chua ?
            End = CheckEnd(row, col);

            TextBlock txtBlock1 = new TextBlock();
            txtBlock1.FontSize = 20;
            txtBlock1.FontWeight = FontWeights.Bold;
            txtBlock1.Foreground = new SolidColorBrush(Colors.Green);
            txtBlock1.VerticalAlignment = VerticalAlignment.Center;
            txtBlock1.HorizontalAlignment = HorizontalAlignment.Center;
            txtBlock1.Text = "O";
            Grid.SetRow(txtBlock1, row - 1);
            Grid.SetColumn(txtBlock1, col - 1);
            LayoutRoot.Children.Add(txtBlock1);
            if (End != Player.None)
            {
                MessageBoxResult re = new MessageBoxResult();
                if (End == Player.Human)
                    re = MessageBox.Show("you win, wanna play again ? ", "", MessageBoxButton.OKCancel);
                if (End == Player.Machine)
                    re = MessageBox.Show("machine win, wanna play again ? ", "", MessageBoxButton.OKCancel);

                if (re == MessageBoxResult.OK)
                {
                    board = new Player[14, 14];
                    LayoutRoot.Children.Clear();
                    fill();
                }
                else
                {
                    Application.Current.Shutdown();
                }

            }
            // run all background tasks here
        }

        private void worker_RunWorkerCompleted(object sender,
                                               RunWorkerCompletedEventArgs e)
        {
            //update ui once worker complete his work
        }

        bool go_done = false;
        private void AutoModeChecked(object sender, RoutedEventArgs e)
        {
            if ((bool)cbOnlMode.IsChecked)
            {
                playmode = 4;
                Random rand = new Random();
                count = rand.Next(4);

                worker.DoWork += worker_DoWork2;
                worker.RunWorkerCompleted += worker_RunWorkerCompleted2;
                worker.RunWorkerAsync();
            }
            else
                playmode = 2;

        }

        private void worker_DoWork2(object sender, DoWorkEventArgs e)
        {
            while (End == Player.None && playmode == 4)
            {
                if (go_done)
                {
                    //MessageBox.Show("may tu choi onl");
                    // Tim nuoc di chien thang.
                    Eboard.Resetboard();
                    GetGenResult();

                    if (Win) // Tim thay.
                    {
                        node = WinMoves[1];
                    }
                    else
                    {
                        Eboard.Resetboard();
                        EvalueGomokuboard(turn);
                        //Thread machineThread = new Thread(new ThreadStart(InitializationThread));


                        node = Eboard.GetMaxNode();
                        if (!Lose)

                            for (int i = 0; i < count; i++)
                            {
                                Eboard.board[node.Row, node.Column] = 0;
                                node = Eboard.GetMaxNode();
                            }

                    }
                    // May di quan.
                    if (node.Row != 0 && node.Column != 0)
                    {
                        row = node.Row;
                        col = node.Column;
                    }
                    else
                        row = col = 6;

                    socket.Emit("MyStepIs", JObject.FromObject(new { player = 2, row = row - 1, col = col - 1 }));
                    go_done = false;
                }
            }
        }

        private void worker_RunWorkerCompleted2(object sender,
                                               RunWorkerCompletedEventArgs e)
        {

            //update ui once worker complete his work
        }

        private void AutoModeUnchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)cbOnlMode.IsChecked)
                playmode = 3;
            else
                playmode = 1;
        }

        private void OnlModeUnchecked(object sender, RoutedEventArgs e)
        {
            socket.Close();
            if ((bool)cbAutoMode.IsChecked)
                playmode = 2;
            else
                playmode = 1;
        }

        string name = "ApoNaruto";

        private void btChangeClick(object sender, RoutedEventArgs e)
        {
            name = tbName.Text;
            if (playmode == 3 || playmode == 4)
            {
                socket.Emit("MyNameIs", name);
            }
            else
            {
                string txt = "Your name now is " + name;
                lbChat.Items.Add(txt);
            }
        }

        private void OnlModeChecked(object sender, RoutedEventArgs e)
        {
            bool auto = false;
            if ((bool)cbAutoMode.IsChecked)
                auto = true;
            else
                auto = false;

            lbChat.Items.Add("waiting for connect...");

            socket = IO.Socket("ws://gomoku-lajosveres.rhcloud.com:8000");
            bool ok = true;

            //while (playmode != 3 && ok)
            //{
            socket.On(Socket.EVENT_CONNECT, () =>
            {
                if (auto)
                    playmode = 4;
                else
                    playmode = 3;
            });

            socket.On(Socket.EVENT_MESSAGE, (data) =>
            {
                MessageBox.Show(data.ToString());
                Debug.WriteLine(data);
            });
            socket.On(Socket.EVENT_CONNECT_ERROR, (data) =>
            {
                Debug.WriteLine(data);
                ok = false;
            });

            //}
            if (!ok)
            {
                lbChat.Items.Add("connect error!");
                if ((bool)cbAutoMode.IsChecked)
                    playmode = 2;
                else
                    playmode = 1;
            }
            else
            {
                name = tbName.Text;
                socket.On("ChatMessage", (data) =>
                {
                    string msg = ((Newtonsoft.Json.Linq.JObject)data)["message"].ToString();
                    string from = "";
                    if (((Newtonsoft.Json.Linq.JObject)data)["from"] != null)
                        from = ((Newtonsoft.Json.Linq.JObject)data)["from"].ToString();
                    else
                        from = "Server";

                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        string text = from + "(" + DateTime.Now.ToString() + "): " + msg;
                        lbChat.Items.Add(text);

                    }));
                    if (((Newtonsoft.Json.Linq.JObject)data)["message"].ToString() == "Welcome!")
                    {
                        socket.Emit("MyNameIs", name);
                        socket.Emit("ConnectToOtherPlayer");
                    }
                    if (((Newtonsoft.Json.Linq.JObject)data)["message"].ToString().Contains("You are the second player!"))
                    {
                        //playfirst = 1;
                        go_done = false;
                    }
                    else if (((Newtonsoft.Json.Linq.JObject)data)["message"].ToString().Contains("You are the first player!"))
                    {
                        //playfirst = 0;
                        go_done = true;
                    }

                    if (((Newtonsoft.Json.Linq.JObject)data)["message"].ToString() == "Invalid step.")
                    {
                        go_done = true;
                    }

                    if (((Newtonsoft.Json.Linq.JObject)data)["message"].ToString().Contains("Not allowed!"))
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            cbOnlMode.IsChecked = false;
                            MessageBox.Show("The player has left the game, please reconnect");
                            socket.Close();
                        }));

                        return;
                    }
                });

                socket.On("NextStepIs", (data) =>
                {
                    Type typeB = data.GetType();
                    row = Int32.Parse(((Newtonsoft.Json.Linq.JObject)data)["row"].ToString());
                    col = Int32.Parse(((Newtonsoft.Json.Linq.JObject)data)["col"].ToString());
                    row++; col++;

                    if (board[row, col] != Player.None)
                        MessageBox.Show("trung nuoc di!");
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        TextBlock txtBlock1 = new TextBlock();

                        if (0 == Int32.Parse(((Newtonsoft.Json.Linq.JObject)data)["player"].ToString())) //luot cua client
                        {
                            txtBlock1.Text = "X";
                            board[row, col] = Player.Human;
                            turn = Player.Machine;
                            //go_done = false;
                        }
                        else
                        {
                            txtBlock1.Text = "O";
                            board[row, col] = Player.Machine;
                            turn = Player.Human;
                            //go_done = true;
                        }
                        txtBlock1.FontSize = 20;
                        txtBlock1.FontWeight = FontWeights.Bold;
                        txtBlock1.Foreground = new SolidColorBrush(Colors.Green);
                        txtBlock1.VerticalAlignment = VerticalAlignment.Center;
                        txtBlock1.HorizontalAlignment = HorizontalAlignment.Center;
                        Grid.SetRow(txtBlock1, row - 1);
                        Grid.SetColumn(txtBlock1, col - 1);
                        LayoutRoot.Children.Add(txtBlock1);

                        End = CheckEnd(row, col);
                        if (End != Player.None)
                        {
                            MessageBoxResult re = new MessageBoxResult();
                            if (End == Player.Human)
                                re = MessageBox.Show("You win, wanna play again ? ", "", MessageBoxButton.OKCancel);
                            if (End == Player.Machine)
                                re = MessageBox.Show("You lose, wanna play again ? ", "", MessageBoxButton.OKCancel);

                            if (re == MessageBoxResult.OK)
                            {
                                board = new Player[14, 14];
                                LayoutRoot.Children.Clear();
                                fill();
                                cbAutoMode.IsChecked = false;
                                cbOnlMode.IsChecked = false;
                                turn = Player.Human;
                                End = Player.None;
                            }
                            else
                            {
                                Application.Current.Shutdown();
                            }
                        }

                        go_done = turn == Player.Machine ? false : true;
                    }));
                });
            }
        }
        private void btSend_Click(object sender, RoutedEventArgs e)
        {
            String text = tbName.Text + "(" + DateTime.Now.ToString() + "): " + tbText.Text;

            if (playmode == 3 || playmode == 4)
            {
                socket.Emit("ChatMessage", tbText.Text);
            }
            else
                lbChat.Items.Add(text);

            tbText.Text = "";
        }
    }
}
