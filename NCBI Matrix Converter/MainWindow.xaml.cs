using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace NCBI_Matrix_Converter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private string _matrix = string.Empty; 

        private void BtnFetch_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtUri.Text.Trim();

            // Check the URL
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Invalid URI");
                return;
            }

            try
            {
                // Fetch the NCBI Scoring Matrix File
                var client = new WebClient();
                _matrix = client.DownloadString(url);
                BtnGenerate.IsEnabled = true;
            }
            catch (WebException wex)
            {
                MessageBox.Show(string.Format("Web Error: {0}", wex.Message));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            string className = TxtClassName.Text.Trim();

            // Check Class Name
            if (string.IsNullOrEmpty(className))
            {
                MessageBox.Show("Invalid Class Name");
                return;
            }
            
            // Remove The comment Lines
            string matrix = Regex.Replace(_matrix.Trim(), "^[ \\\\t]*#.*$[\r\n]*", string.Empty, RegexOptions.Multiline);
            // Split the First Line
            string firstLine = matrix.Substring(0, matrix.IndexOf('\n'));
            matrix = matrix.Remove(0, matrix.IndexOf('\n'));
            matrix = matrix.TrimStart();

            // Replace heading & tailing symbols with { & },
            matrix = Regex.Replace(matrix, "[A|R|N|D|C|Q|E|G|H|I|L|K|M|F|P|S|T|W|Y|V|B|Z|X|*]", "{");
            matrix = Regex.Replace(matrix, "$", "},", RegexOptions.Multiline);

            // Split the Last Line
            string lastLine = matrix.Substring(matrix.LastIndexOf('{'), matrix.LastIndexOf(',')- matrix.LastIndexOf('{')+1);
            matrix = matrix.Remove(matrix.LastIndexOf(",\n{"), matrix.LastIndexOf(',')- matrix.LastIndexOf('{')+3);
            matrix = matrix.TrimEnd();

            // Format the matrix to Array format
            matrix = Regex.Replace(matrix, "(-?[0-9]+)(\\s+)(-?[0-9]+)", "$1$2,$3,");
            
            // Get matrix symbols
            var matches = Regex.Matches(firstLine.Trim(), "([A|R|N|D|C|Q|E|G|H|I|L|K|M|F|P|S|T|W|Y|V|B|Z|X])");
            var symbols = new StringBuilder();

            // place symbols into dictionary format
            for (int i=0; i<matches.Count; i++)
            {
                symbols.AppendLine(string.Format("\t\t{{\'{0}\', {1}}}{2}", matches[i].Value, i, i<matches.Count-1?",":""));
            }

            // Get Wild Match & Mismatch Scores
            var wildMatches = Regex.Matches(lastLine, "(-?[0-9]+)\\s+(-?[0-9]+)\\s*},$");
            int wildMismatch = -1, wildMatch = 1;

            if (wildMatches.Count > 0)
            {
                wildMismatch = int.Parse(wildMatches[0].Groups[1].Value);
                wildMatch = int.Parse(wildMatches[0].Groups[2].Value);
            }

            matrix = Regex.Replace(matrix, $",\\s*{wildMismatch}\\s*,\\s*}}", "}");

            // Format the Output Template
            TxtOutput.Text = string.Format(@Templates.DefaultTemplate,
                className,
                TxtUri.Text,
                _matrix,
                symbols,
                matrix,
                wildMismatch,
                wildMatch);
        }
    }
}
