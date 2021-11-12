using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace Assignment2
{
    public class Feed
    {
        private static HttpClient http = new HttpClient();
        public string Url;
        public string Title;

        //because we always load a document associated with a feed, we thought it made sense to put this as an instance method.
        public async Task<XDocument> LoadDocumentAsync()
        {
            // This is just to simulate a slow/large data transfer and make testing easier.
            // Remove it if you want to.
            await Task.Delay(1000);
            var response = await http.GetAsync(this.Url);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();
            var feedDocument = XDocument.Load(stream);
            return feedDocument;
        }
    }
    public class Article
    {
        public Feed Feed;
        public string Title;
        public DateTime DateTime;

    }

    public partial class MainWindow : Window
    {
        private Thickness spacing = new Thickness(5);
        
        // We will need these as instance variables to access in event handlers.
        private TextBox addFeedTextBox;
        private Button addFeedButton;
        private ComboBox selectFeedComboBox;
        private Button loadArticlesButton;
        private StackPanel articlePanel;
        public List<Feed> feeds = new List<Feed>();

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        private void Start()
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
            addFeedButton.Click += HandleAddFeedClickAsync;

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
            loadArticlesButton.Click += HandleLoadArticlesClickAsync;

            articlePanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = spacing
            };
            grid.Children.Add(articlePanel);
            Grid.SetRow(articlePanel, 2);
            Grid.SetColumnSpan(articlePanel, 3);

            var userInput = addFeedTextBox.Text;
           
        }

        private async void HandleAddFeedClickAsync(object sender, RoutedEventArgs e)
        {
            addFeedButton.IsEnabled = false;

            await Task.Delay(1000);

            var url = addFeedTextBox.Text;
            var feed = await CreateFeedAsync(url);
            feeds.Add(feed);

            var newItem = new ComboBoxItem();
            newItem.Content = feed.Title;
            newItem.Tag = feed;

            selectFeedComboBox.Items.Add(newItem);
            addFeedButton.IsEnabled = true;

        }

        private async void HandleLoadArticlesClickAsync(object sender, RoutedEventArgs e)
        {
            articlePanel.Children.Clear();

            //index of 0 is the "show all feeds" option
            if (selectFeedComboBox.SelectedIndex == 0) {

                //all articles to load be loaded
                var allArticlesTasks = feeds.Select(LoadArticlesAsync).ToList();

                //results will be stored here
                var allArticles = new List<Article>();

                while (allArticlesTasks.Count > 0)
                {
                    var task = await Task.WhenAny(allArticlesTasks);
                    allArticles.AddRange(await task);
                    allArticlesTasks.Remove(task);
                }
                //sort & display
                ShowArticles(allArticles.OrderByDescending(x => x.DateTime).ToList());
            } 
            else
            {
                //the first item in our combobox contains the "show all feeds" text, so we need to subtract 1 from the selected index for it to match our list of feeds.
                var selectedFeed = feeds[selectFeedComboBox.SelectedIndex - 1];
                var items = await LoadArticlesAsync(selectedFeed);
                ShowArticles(items);
            }
        }

        private async Task<Feed> CreateFeedAsync(string url)
        {
            var feed = new Feed();
            feed.Url = url;
            feed.Title = (await feed.LoadDocumentAsync()).Descendants("title").First().Value;

            return feed;
        }

        private async Task<IEnumerable<Article>> LoadArticlesAsync(Feed feed)
        {
            //we disable button while loading
            loadArticlesButton.IsEnabled = false;

            //the article data is in the <item> tags of the Xdocument
            var itemsXElements = (await feed.LoadDocumentAsync()).Descendants("item");

            //parse each item and add to list of articles
            var articles = new List<Article>();
            foreach (var xElement in itemsXElements)
            {
                var article = new Article
                {
                    Feed = feed,
                    Title = xElement.Descendants("title").First().Value,
                    DateTime = DateTime.ParseExact(xElement.Descendants("pubDate").First().Value.Substring(0, 25),
                        "ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                };
                articles.Add(article);
            }

            loadArticlesButton.IsEnabled = true;

            return articles;
        }

        private void ShowArticles(IEnumerable<Article> articles)
        {
            foreach (Article article in articles)
            {
                var articlePlaceholder = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = spacing
                };
                articlePanel.Children.Add(articlePlaceholder);

                var articleTitle = new TextBlock
                {
                    Text = article.DateTime + " - " + article.Title,
                    FontWeight = FontWeights.Bold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                articlePlaceholder.Children.Add(articleTitle);

                var articleWebsite = new TextBlock
                {
                    Text = article.Feed.Title
                };
                articlePlaceholder.Children.Add(articleWebsite);
            }
        }
    }
}
