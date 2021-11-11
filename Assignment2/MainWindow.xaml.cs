using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
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
using System.Xml.Linq;

namespace Assignment2
{
    public class Feed
    {
        public string Url;
        public string Title;
        public XDocument Document;
    }
    public class Item
    {
        public Feed Feed;
        public string Title;
        public DateTime DateTime;

    }

    public partial class MainWindow : Window
    {
        private Thickness spacing = new Thickness(5);
        private HttpClient http = new HttpClient();
        // We will need these as instance variables to access in event handlers.
        private TextBox addFeedTextBox;
        private Button addFeedButton;
        private ComboBox selectFeedComboBox;
        private Button loadArticlesButton;
        private StackPanel articlePanel;
        public List<Feed> feeds = new List<Feed>();
        private string url;

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        private async Task Start()
        {
            // Window options
            Title = "Feed Reader";
            Width = 800;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Scrolling
            var root = new ScrollViewer();
            root.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            Content = root;

            // Main grid
            var grid = new Grid();
            root.Content = grid;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var addFeedLabel = new Label
            {
                Content = "Feed URL:",
                Margin = spacing
            };
            grid.Children.Add(addFeedLabel);

            addFeedTextBox = new TextBox
            {
                Margin = spacing,
                Padding = spacing
            };
            grid.Children.Add(addFeedTextBox);
            Grid.SetColumn(addFeedTextBox, 1);

            addFeedButton = new Button
            {
                Content = "Add Feed",
                Margin = spacing,
                Padding = spacing
            };
            grid.Children.Add(addFeedButton);
            Grid.SetColumn(addFeedButton, 2);
            addFeedButton.Click += HandleAddFeedClick;

            var selectFeedLabel = new Label
            {
                Content = "Select Feed:",
                Margin = spacing
            };
            grid.Children.Add(selectFeedLabel);
            Grid.SetRow(selectFeedLabel, 1);

            selectFeedComboBox = new ComboBox
            {
                Margin = spacing,
                Padding = spacing,
                IsEditable = false
            };
            selectFeedComboBox.Items.Add("All feeds");
            selectFeedComboBox.SelectedIndex = 0;
            grid.Children.Add(selectFeedComboBox);
            Grid.SetRow(selectFeedComboBox, 1);
            Grid.SetColumn(selectFeedComboBox, 1);

            loadArticlesButton = new Button
            {
                Content = "Load Articles",
                Margin = spacing,
                Padding = spacing
            };
            grid.Children.Add(loadArticlesButton);
            Grid.SetRow(loadArticlesButton, 1);
            Grid.SetColumn(loadArticlesButton, 2);
            loadArticlesButton.Click += HandleLoadArticlesButtonClick;

            articlePanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = spacing
            };
            grid.Children.Add(articlePanel);
            Grid.SetRow(articlePanel, 2);
            Grid.SetColumnSpan(articlePanel, 3);

            var userInput = addFeedTextBox.Text;

            // These are just placeholders.
            // Replace them with your own code that shows actual articles.
           
        }

        private void HandleLoadArticlesButtonClick(object sender, RoutedEventArgs e)
        {
            loadArticlesButton.IsEnabled = false;

            articlePanel.Children.Clear();

            if (selectFeedComboBox.SelectedIndex == 0)
            {
                ShowAllFeeds();
            }
            else
            {
                var items = GetItems(feeds[selectFeedComboBox.SelectedIndex - 1]).ToList();

                foreach (Item item in items)
                {
                    var articlePlaceholder = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Margin = spacing
                    };
                    articlePanel.Children.Add(articlePlaceholder);

                    var articleTitle = new TextBlock
                    {
                        Text = item.DateTime + " - " + item.Title,
                        FontWeight = FontWeights.Bold,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    articlePlaceholder.Children.Add(articleTitle);

                    var articleWebsite = new TextBlock
                    {
                        Text = item.Feed.Title
                    };
                    articlePlaceholder.Children.Add(articleWebsite);
                }
            }
        }

        private async void ShowAllFeeds()
        {
            var allItems = new List<Item>();

            var feedUrl = feeds.Select(f => f.Url).ToList();
            var feedTasks = feedUrl.Select(GetFeed).ToList();

            while (feedTasks.Count > 0)
            {
                var task = await Task.WhenAny(feedTasks);
                allItems.AddRange(GetItems(await task));
                feedTasks.Remove(task);
            }

            allItems = allItems.OrderByDescending(x => x.DateTime).ToList();


            foreach (Item item in allItems)
            {
                var articlePlaceholder = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = spacing
                };
                articlePanel.Children.Add(articlePlaceholder);

                var articleTitle = new TextBlock
                {
                    Text = item.DateTime + " - " + item.Title,
                    FontWeight = FontWeights.Bold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                articlePlaceholder.Children.Add(articleTitle);

                var articleWebsite = new TextBlock
                {
                    Text = item.Feed.Title
                };
                articlePlaceholder.Children.Add(articleWebsite);
            }
        }

        private void HandleAddFeedClick(object sender, RoutedEventArgs e)
        {
            addFeedButton.IsEnabled = false;
            AddFeed();
        }


        private async Task<XDocument> LoadDocumentAsync(string url)
        {
            // This is just to simulate a slow/large data transfer and make testing easier.
            // Remove it if you want to.
            await Task.Delay(1000);
            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();
            var feed = XDocument.Load(stream);
            return feed;
        }

        private async void AddFeed()
        {
            await Task.Delay(1000);

            var url = addFeedTextBox.Text;
            var feed = await GetFeed(url);
            feeds.Add( feed);

            var newItem = new ComboBoxItem();
            newItem.Content = feed.Title;
            newItem.Tag = feed;

            selectFeedComboBox.Items.Add(newItem);
            addFeedButton.IsEnabled = true;

        }
        private async Task<Feed> GetFeed(string url)
        {

            var documentTask = LoadDocumentAsync(url);
            var document = await documentTask;

            var feed = new Feed();

            feed.Title = document.Descendants("title").First().Value;
            feed.Url = url;
            feed.Document = document;

            return feed;
        }

        private IEnumerable<Item> GetItems(Feed feed)
        {

            var itemsXElements = feed.Document.Descendants("item");
            var items = new List<Item>();

            foreach (var xElement in itemsXElements)
            {
                var item = new Item
                {
                    Feed = feed,
                    Title = xElement.Descendants("title").First().Value,
                    DateTime = DateTime.ParseExact(xElement.Descendants("pubDate").First().Value.Substring(0, 25),
                        "ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                };
                items.Add(item);
            }

            return items;
        }
    }
}
