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

		// ��Activity����������ʱ
		protected override void OnDestroy()
		{
			if (_mediaPlayer != null)
			{
				_mediaPlayer.Stop();
				// �ͷ�MediaPlayer��ռ�õ�������Դ�����Ǳ���ģ�
				_mediaPlayer.Release();
				_mediaPlayer = null;
			}
			base.OnDestroy();
		}

		#region Initialization
		private void InitializeMediaPlayer()
		{
			// ʹ�þ�̬�������� MediaPlayer.Create ������һ��ʵ�������׼������
			// �����Զ����� new, setDataSource, prepare �Ȳ���
			_mediaPlayer = MediaPlayer.Create(this, Resource.Raw.welcome);

			// ��ʼ����
			_mediaPlayer.Start();
		}

		//�Ӳ����в��Ҳ�������UI�ؼ���������
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

		//��ʼ�������б��������������
		private void InitializeAdapter()
		{
			_generatedNumbers = new List<int>();
			//�������󶨵��������ַ����б������ʾ��
			var displayList = new List<string>();
			_adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, displayList);
			_resultsListView.Adapter = _adapter;
		}

		// ���¼��������󶨵�UI�ؼ���
		private void InitializeEvents()
		{
			_generateButton.Click += OnGenerateButtonClick;
			RegisterForContextMenu(_resultsListView);
		}

		#endregion

		#region Core Logic

		// ����"����"��ť����¼�
		private void OnGenerateButtonClick(object sender, EventArgs e)
		{
			if (!ValidateInputsAndParse(out int min, out int max, out int count))
			{
				return;  // ��֤�д���,��ֹ���ִ�С�
			}

			bool allowDuplicates = _allowDuplicatesCheckBox.Checked;
			_generatedNumbers = GenerateRandomNumbers(min, max, count, allowDuplicates);

			// Ĭ������ģʽΪ����
			_currentSortModeId = Resource.Id.context_menu_sort_shuffle;

			ApplySort();
			UpdateStatusText(count, min, max, allowDuplicates);
			UpdateListView();
		}

		/// <summary>
		/// ��֤�û����벢����Ϊ������
		/// </summary>
		/// <returns>����������붼��Ч����ΪTrue������Ϊfalse��</returns>
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

			// ��Χ���� >= 0
			if (min < 0) min = 0;

			if (min >= max)
			{
				ShowToast(GetString(Resource.String.toast_min_max_error));
				return false;
			}

			// ����������1~100����
			if (count < 1 || count > 100)
			{
				ShowToast(GetString(Resource.String.toast_count_error));
				return false;
			}

			// ��max�ӽ�int.MaxValueʱ��ʹ��long�Է�ֹ���
			long range = (long)max - min + 1;
			if (!_allowDuplicatesCheckBox.Checked && count > range)
			{
				ShowToast(GetString(Resource.String.toast_range_error));
				return false;
			}

			return true;
		}

		/// <summary>
		/// ���ݸ�����������������б�
		/// </summary>
		/// <returns>һ���µ���������б�</returns>
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
		/// ���ݵ�ǰ����ģʽ�����ɵ����ֵ��ڲ��б��������
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
		/// ͨ��������������������������Դ������ListView��
		/// </summary>
		private void UpdateListView()
		{
			_adapter.Clear();
			_adapter.AddAll(_generatedNumbers.Select(n => n.ToString()).ToList());
			_adapter.NotifyDataSetChanged();
		}

		// ���½��״̬�ı�
		private void UpdateStatusText(int count, int min, int max, bool allowDuplicates)
		{
			// �����Ƿ������ظ���ѡ���Ӧ���ַ�����Դ
			string duplicatesStatus = allowDuplicates
							? GetString(Resource.String.status_with_duplicates)
							: GetString(Resource.String.status_without_duplicates);

			// ʹ�ø�ʽ���ַ���ģ�壬���������
			string statusText = GetString(Resource.String.status_generation_success, count, min, max, duplicatesStatus);

			_statusTextView.Text = statusText;
		}

		#endregion

		#region Context Menu

		public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
		{
			base.OnCreateContextMenu(menu, v, menuInfo);
			MenuInflater.Inflate(Resource.Menu.list_context_menu, menu);

			//��̬���¸��˵���ı���
			var parentSortMenuItem = menu.FindItem(Resource.Id.context_menu_sort);
			if (parentSortMenuItem != null && parentSortMenuItem.HasSubMenu)
			{
				var currentSortSubMenuItem = parentSortMenuItem.SubMenu.FindItem(_currentSortModeId);
				if (currentSortSubMenuItem != null)
				{
					parentSortMenuItem.SetTitle($"{GetString(Resource.String.menu_sort_by)}��{currentSortSubMenuItem.TitleFormatted}");
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
					// ����ʽ���ƣ��� [1,5,3]
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
		/// ��ʱ����ʾToast֪ͨ��
		/// </summary>
		private void ShowToast(string message)
		{
			Toast.MakeText(this, message, ToastLength.Long).Show();
		}

		/// <summary>
		/// ��ָ���ı����Ƶ�ϵͳ�����塣
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
