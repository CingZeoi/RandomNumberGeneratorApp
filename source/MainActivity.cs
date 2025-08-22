using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RandomNumberGenerator
{
	[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@android:style/Theme.DeviceDefault.Light")]
	public class MainActivity : Activity
	{
		#region UI Controls

		private EditText _minEditText;
		private EditText _maxEditText;
		private EditText _countEditText;
		private CheckBox _allowDuplicatesCheckBox;
		private Button _generateButton;
		private TextView _statusTextView;
		private ListView _resultsListView;

		#endregion

		#region Media Player
		private MediaPlayer _mediaPlayer;
		#endregion

		#region Data Models

		private List<int> _generatedNumbers;
		private ArrayAdapter<string> _adapter;
		private int _currentSortModeId = Resource.Id.context_menu_sort_shuffle;

		#endregion

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			SetContentView(Resource.Layout.activity_main);
			InitializeViews();
			InitializeAdapter();
			InitializeEvents();
			InitializeMediaPlayer();
		}

		// 当Activity即将被销毁时
		protected override void OnDestroy()
		{
			if (_mediaPlayer != null)
			{
				_mediaPlayer.Stop();
				// 释放MediaPlayer所占用的所有资源！这是必须的！
				_mediaPlayer.Release();
				_mediaPlayer = null;
			}
			base.OnDestroy();
		}

		#region Initialization
		private void InitializeMediaPlayer()
		{
			// 使用静态工厂方法 MediaPlayer.Create 来创建一个实例并完成准备工作
			// 它会自动处理 new, setDataSource, prepare 等步骤
			_mediaPlayer = MediaPlayer.Create(this, Resource.Raw.welcome);

			// 开始播放
			_mediaPlayer.Start();
		}

		//从布局中查找并绑定所有UI控件到变量。
		private void InitializeViews()
		{
			_minEditText = FindViewById<EditText>(Resource.Id.minEditText);
			_maxEditText = FindViewById<EditText>(Resource.Id.maxEditText);
			_countEditText = FindViewById<EditText>(Resource.Id.countEditText);
			_allowDuplicatesCheckBox = FindViewById<CheckBox>(Resource.Id.allowDuplicatesCheckBox);
			_generateButton = FindViewById<Button>(Resource.Id.generateButton);
			_statusTextView = FindViewById<TextView>(Resource.Id.statusTextView);
			_resultsListView = FindViewById<ListView>(Resource.Id.resultsListView);
		}

		//初始化数据列表和数组适配器。
		private void InitializeAdapter()
		{
			_generatedNumbers = new List<int>();
			//适配器绑定到单独的字符串列表进行显示。
			var displayList = new List<string>();
			_adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, displayList);
			_resultsListView.Adapter = _adapter;
		}

		// 将事件处理器绑定到UI控件。
		private void InitializeEvents()
		{
			_generateButton.Click += OnGenerateButtonClick;
			RegisterForContextMenu(_resultsListView);
		}

		#endregion

		#region Core Logic

		// 处理"生成"按钮点击事件
		private void OnGenerateButtonClick(object sender, EventArgs e)
		{
			if (!ValidateInputsAndParse(out int min, out int max, out int count))
			{
				return;  // 验证有错误,终止向后执行。
			}

			bool allowDuplicates = _allowDuplicatesCheckBox.Checked;
			_generatedNumbers = GenerateRandomNumbers(min, max, count, allowDuplicates);

			// 默认排序模式为乱序
			_currentSortModeId = Resource.Id.context_menu_sort_shuffle;

			ApplySort();
			UpdateStatusText(count, min, max, allowDuplicates);
			UpdateListView();
		}

		/// <summary>
		/// 验证用户输入并解析为整数。
		/// </summary>
		/// <returns>如果所有输入都有效，则为True，否则为false。</returns>
		private bool ValidateInputsAndParse(out int min, out int max, out int count)
		{
			min = 0; max = 0; count = 0;

			if (!int.TryParse(_minEditText.Text, out min) ||
							!int.TryParse(_maxEditText.Text, out max) ||
							!int.TryParse(_countEditText.Text, out count))
			{
				ShowToast(GetString(Resource.String.toast_invalid_input));
				return false;
			}

			// 范围必须 >= 0
			if (min < 0) min = 0;

			if (min >= max)
			{
				ShowToast(GetString(Resource.String.toast_min_max_error));
				return false;
			}

			// 数量限制在1~100以内
			if (count < 1 || count > 100)
			{
				ShowToast(GetString(Resource.String.toast_count_error));
				return false;
			}

			// 当max接近int.MaxValue时，使用long以防止溢出
			long range = (long)max - min + 1;
			if (!_allowDuplicatesCheckBox.Checked && count > range)
			{
				ShowToast(GetString(Resource.String.toast_range_error));
				return false;
			}

			return true;
		}

		/// <summary>
		/// 根据给定参数生成随机数列表。
		/// </summary>
		/// <returns>一个新的随机整数列表。</returns>
		private List<int> GenerateRandomNumbers(int min, int max, int count, bool allowDuplicates)
		{
			var numbers = new List<int>(count);
			var random = new Random();

			if (allowDuplicates)
			{
				for (int i = 0; i < count; i++)
				{
					numbers.Add(random.Next(min, max));
				}
			}
			else
			{
				var uniqueNumbers = new HashSet<int>();
				while (uniqueNumbers.Count < count)
				{
					uniqueNumbers.Add(random.Next(min, max + 1));
				}
				numbers.AddRange(uniqueNumbers);
			}
			return numbers;
		}

		#endregion

		#region UI Update & Sorting

		/// <summary>
		/// 根据当前排序模式对生成的数字的内部列表进行排序。
		/// </summary>
		private void ApplySort()
		{
			switch (_currentSortModeId)
			{
				case Resource.Id.context_menu_sort_asc:
					_generatedNumbers.Sort();
					break;
				case Resource.Id.context_menu_sort_desc:
					_generatedNumbers.Sort();
					_generatedNumbers.Reverse();
					break;
				case Resource.Id.context_menu_sort_shuffle:
					var random = new Random();
					_generatedNumbers = _generatedNumbers.OrderBy(x => random.Next()).ToList();
					break;
			}
		}

		/// <summary>
		/// 通过清除并重新填充适配器的数据源来更新ListView。
		/// </summary>
		private void UpdateListView()
		{
			_adapter.Clear();
			_adapter.AddAll(_generatedNumbers.Select(n => n.ToString()).ToList());
			_adapter.NotifyDataSetChanged();
		}

		// 更新结果状态文本
		private void UpdateStatusText(int count, int min, int max, bool allowDuplicates)
		{
			// 根据是否允许重复，选择对应的字符串资源
			string duplicatesStatus = allowDuplicates
							? GetString(Resource.String.status_with_duplicates)
							: GetString(Resource.String.status_without_duplicates);

			// 使用格式化字符串模板，并填充数据
			string statusText = GetString(Resource.String.status_generation_success, count, min, max, duplicatesStatus);

			_statusTextView.Text = statusText;
		}

		#endregion

		#region Context Menu

		public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
		{
			base.OnCreateContextMenu(menu, v, menuInfo);
			MenuInflater.Inflate(Resource.Menu.list_context_menu, menu);

			//动态更新父菜单项的标题
			var parentSortMenuItem = menu.FindItem(Resource.Id.context_menu_sort);
			if (parentSortMenuItem != null && parentSortMenuItem.HasSubMenu)
			{
				var currentSortSubMenuItem = parentSortMenuItem.SubMenu.FindItem(_currentSortModeId);
				if (currentSortSubMenuItem != null)
				{
					parentSortMenuItem.SetTitle($"{GetString(Resource.String.menu_sort_by)}：{currentSortSubMenuItem.TitleFormatted}");
					currentSortSubMenuItem.SetChecked(true);
				}
			}
		}

		public override bool OnContextItemSelected(IMenuItem item)
		{
			var info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;

			switch (item.ItemId)
			{
				case Resource.Id.context_menu_copy_item:
					int numberToCopy = _generatedNumbers[info.Position];
					CopyToClipboard("Selected Number", numberToCopy.ToString());
					ShowToast(GetString(Resource.String.toast_copied_item, numberToCopy));
					return true;

				case Resource.Id.context_menu_copy_all:
					// 按格式复制，如 [1,5,3]
					string allNumbers = $"[{string.Join(",", _generatedNumbers)}]";
					CopyToClipboard("All Numbers", allNumbers);
					ShowToast(GetString(Resource.String.toast_copied_all));
					return true;

				case Resource.Id.context_menu_clear:
					_generatedNumbers.Clear();
					UpdateListView();
					_statusTextView.Text = GetString(Resource.String.status_empty_list);
					ShowToast(GetString(Resource.String.toast_cleared));
					return true;

				case Resource.Id.context_menu_sort_shuffle:
				case Resource.Id.context_menu_sort_asc:
				case Resource.Id.context_menu_sort_desc:
					item.SetChecked(true);

					_currentSortModeId = item.ItemId;
					ApplySort();
					UpdateListView();
					return true;

				default:
					return base.OnContextItemSelected(item);
			}
		}

		#endregion

		#region Utility Methods

		/// <summary>
		/// 长时间显示Toast通知。
		/// </summary>
		private void ShowToast(string message)
		{
			Toast.MakeText(this, message, ToastLength.Long).Show();
		}

		/// <summary>
		/// 将指定文本复制到系统剪贴板。
		/// </summary>
		private void CopyToClipboard(string label, string text)
		{
			var clipboard = (ClipboardManager)GetSystemService(ClipboardService);
			var clip = ClipData.NewPlainText(label, text);
			if (clipboard != null)
			{
				clipboard.PrimaryClip = clip;
			}
		}
		#endregion
	}
}
