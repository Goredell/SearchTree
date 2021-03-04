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
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Diagnostics;

namespace SearchTree
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		string[] saveload = new string[] { "", "" };
		bool Pause = false;
		int found = 0,
		 count = 0;
		string strfound = "0",
			strcount = "0";
		Stopwatch stopWatch = new Stopwatch();
		public MainWindow()
		{
			InitializeComponent();

			try
			{
				StreamReader reader = new StreamReader("save.dat");
				saveload[0] = reader.ReadLine();
				saveload[1] = reader.ReadLine();
				reader.Close();
			}
			catch (Exception)
			{
				saveload[0] = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			}
			finally
			{
				DirectoryBox.Text = saveload[0];
				SearchBox.Text = saveload[1];
			}

		}
		private void Browse_button_Click(object sender, RoutedEventArgs e)
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			if (Directory.Exists(DirectoryBox.Text))
				dlg.SelectedPath = DirectoryBox.Text;
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				// Open document
				DirectoryBox.Text = dlg.SelectedPath; //Add validation?
			}

		}

		private async void Search_button_Click(object sender, RoutedEventArgs e)
		{
			Thread.CurrentThread.Name = "Main";

			this.Pause_Button.IsEnabled = true;
			found = 0;
			count = 0;
			labelFound.Content = found;
			labelCount.Content = count;
			string search = SearchBox.Text,
				   direct = DirectoryBox.Text;
			var tasks = new List<Task>();
			foreach (var task in tasks)
			{
				task.Dispose();
			}
			SearchTree.Items.Clear();


			var timer = new System.Windows.Threading.DispatcherTimer();
			timer.Tick += new EventHandler(Timer_Tick);
			timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
			timer.Start();
			stopWatch.Start();

			foreach (string drive in Directory.GetLogicalDrives())
			{
				TreeViewItem item = new TreeViewItem();
				item.Header = drive.Substring(0, drive.IndexOf("\\"));
				//item.Tag = ;
				SearchTree.Items.Add(item);
			}


			Regex reg = new Regex("");
			try
			{
				reg = new Regex(search);
			}
			catch (ArgumentException e1)
			{
				System.Windows.MessageBox.Show(e1.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			tasks.Add(Task.Run(() =>
			{
				var files = Directory.GetFiles(direct)
						 .Where(path => reg.IsMatch(path))
						 .ToList();

				build_starter(SearchTree, files);
				var dir = Directory.GetDirectories(direct);
				foreach (var di in dir)
					searcher(di, reg);

			}));

			Task t = Task.WhenAll(tasks.ToArray());
			try
			{
				await t;
			}
			catch { }
			finally
			{
				if (t.Status == TaskStatus.RanToCompletion)
				{
					timer.Stop();
					//stopWatch.Stop();
				}
			}
		}
		private void Timer_Tick(object sender, EventArgs e)
		{
			TimeSpan ts = stopWatch.Elapsed;
			string currentTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
			TimerLabel.Content = currentTime;


			// Forcing the CommandManager to raise the RequerySuggested event
			CommandManager.InvalidateRequerySuggested();
		}

		async void searcher(string directory, Regex reg)
		{
			await Task.Run(() =>
			{
				//Передать текущую директорию поиска
				//this.Dispatcher.Invoke(() =>
				//{
				//    this.searchLabel.Content = directory;
				//});

				//Поиск файлов и директорий в текущей директории
				List<string> listOfFiles = new List<string>();
				try
				{
					listOfFiles = Directory.GetFiles(directory).ToList();
					if (listOfFiles.Count > 0)
					{
						count += listOfFiles.Count;
						strcount = count.ToString();

						listOfFiles = listOfFiles.Where(path => reg.IsMatch(path))
										.ToList();
						found += listOfFiles.Count();
						strfound = found.ToString();
						this.Dispatcher.Invoke(() =>
						{
							//Данные о просмотренных файлах
							labelCount.Content = strcount;
						});
						if (listOfFiles.Count > 0)
						{
							this.Dispatcher.Invoke(() =>
							{
								//Данные о найденных файлах
								labelFound.Content = strfound;
								//Построить и отобразить древо поиска
							});
							build_starter(SearchTree, listOfFiles);
						}
					}
				}
				//Отлов исключений на отсутсвие доступа
				catch (System.UnauthorizedAccessException) { }
			});

			await Task.Run(() =>
			{
				//
				//Список директорий в этой директории
				string[] dir = { };
				try { dir = Directory.GetDirectories(directory); }
				//Отлов исключений на отсутсвие доступа
				catch (System.UnauthorizedAccessException) { }


				foreach (var di in dir)
					searcher(di, reg);
			});
		}

		void build_starter(ItemsControl root, List<string> path)
		{
			if (path.Count() == 0)
				return;

			TreeViewItem Node = null;
			int strIndex = path[0].IndexOf("\\");


			this.Dispatcher.Invoke(() =>
			{
				foreach (TreeViewItem item in root.Items)
					if (item.Header.ToString() == path[0].Substring(0, strIndex))
						Node = buildpath(item, path[0].Substring(strIndex + 1));

				if (Node != null)
					foreach (string name in path)
					{
						var item = new TreeViewItem();
						item.Header = name.Substring(name.LastIndexOf("\\") + 1);
						item.Tag = (Node.Tag == null || Node.Tag.ToString() == "" ? "" : Node.Tag + "\\") + Node.Header;
						Node.Items.Add(item);
					}
			});
		}
		TreeViewItem buildpath(TreeViewItem Node, string path)
		{
			if (path == "")
				return null;

			string name;
			if (path.IndexOf("\\") == -1)
				return Node;
			else
				name = path.Substring(0, path.IndexOf("\\"));

			foreach (TreeViewItem titem in Node.Items)
				if (titem.Header.ToString() == name)
					return buildpath(titem, path.Substring(path.IndexOf("\\") + 1));


			TreeViewItem item = new TreeViewItem();
			item.Header = name;
			item.Tag = (Node.Tag == null || Node.Tag.ToString() == "" ? "" : Node.Tag + "\\") + Node.Header;
			Node.Items.Add(item);

			return buildpath(item, path.Substring(path.IndexOf("\\") + 1));

		}




		private void DirectoryBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			saveload[0] = DirectoryBox.Text;
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			saveload[1] = SearchBox.Text;
		}

		private void SearchTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			TreeViewItem tvi = this.SearchTree.SelectedItem as TreeViewItem;
			if (tvi != null && tvi.Items.Count == 0)
			{
				string s = tvi.Tag == null ? tvi.Header.ToString() : "/Select, \"" + tvi.Tag.ToString() + "\\" + tvi.Header.ToString() + "\"";
				System.Diagnostics.Process.Start("explorer.exe", s);
			}
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			StreamWriter writer = new StreamWriter("save.dat");
			writer.WriteLine(saveload[0]);
			writer.WriteLine(saveload[1]);
			writer.Close();

			base.OnClosing(e);
		}

		private void Pause_Click(object sender, RoutedEventArgs e)
		{
			Pause = !Pause;
			if (Pause)
			{
				this.Pause_Button.Content = "Continue";
			}
			else
			{
				this.Pause_Button.Content = "Pause";
			}
		}





		//async Task<List<System.Windows.Controls.TreeViewItem>> build_starterAsync(List<System.Windows.Controls.TreeViewItem> root, List<string> path)
		//{
		//	List<System.Windows.Controls.TreeViewItem> ret = root;
		//	TreeViewItem Node = null;
		//	if (path.Count() > 0)
		//	{
		//		foreach (TreeViewItem item in ret)
		//			if (item.Header.ToString() == path[0].Substring(0, path[0].IndexOf("\\") + 1))
		//			{
		//				Node = buildpath(item, path[0].Substring(path[0].IndexOf("\\") + 1));
		//			}
		//		if (Node != null)
		//			foreach (string name in path)
		//			{
		//				TreeViewItem item = new TreeViewItem();
		//				item.Header = name.Substring(name.LastIndexOf("\\") + 1);
		//				Node.Items.Add(item);
		//			}
		//	}
		//	return ret;
		//}

		//void build_starter(ItemsControl root, string path)
		//{
		//	foreach (TreeViewItem item in root.Items)
		//		if (item.Header.ToString() == path.Substring(0, path.IndexOf("\\") + 1))
		//		{
		//			builder(item, path.Substring(path.IndexOf("\\") + 1));
		//		}
		//}


		//void builder(TreeViewItem Node, string path)
		//{
		//	bool got_it = false;
		//
		//	if (path != "")
		//	{
		//		string name;
		//		if (path.IndexOf("\\") == -1)
		//		{
		//			name = path;
		//			path = "";
		//		}
		//		else
		//			name = path.Substring(0, path.IndexOf("\\"));
		//
		//		foreach (TreeViewItem item in Node.Items)
		//		{
		//			if (item.Header.ToString() == name)
		//			{
		//				got_it = true;
		//				builder(item, path.Substring(path.IndexOf("\\") + 1));
		//			}
		//		}
		//
		//		if (!got_it)
		//		{
		//			TreeViewItem item = new TreeViewItem();
		//			item.Header = name;
		//			Node.Items.Add(item);
		//
		//			builder(item, path.Substring(path.IndexOf("\\") + 1));
		//		}
		//	}
		//}
	}
}
