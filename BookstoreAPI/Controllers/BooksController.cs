using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace BookstoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private static List<Book> books;
        private readonly string _xmlFilePath;

        public BooksController(IConfiguration configuration, IHostEnvironment environment)
        {
            var relativePath = configuration["XmlFilePath"];
            _xmlFilePath = Path.Combine(environment.ContentRootPath, relativePath);
            books = LoadBooksFromXml(_xmlFilePath);
        }

        [HttpGet]
        public IActionResult GetAllBooks()
        {
            return Ok(books);
        }

        [HttpGet("{isbn}")]
        public IActionResult GetBookByISBN(string isbn)
        {
            var bookExist = books.FirstOrDefault(book => book.ISBN == isbn);
            if (bookExist == null)
                return NotFound();
            return Ok(bookExist);
        }

        [HttpGet("report")]
        public IActionResult GetBooksReport()
        {
            var stringBuild = new StringBuilder();
            stringBuild.Append("<html><body><h1>Books Report</h1><table border='1'><tr><th>ISBN</th><th>Title</th><th>Authors</th><th>Category</th><th>Year</th><th>Price</th></tr>");

            foreach (var book in books)
            {
                stringBuild.Append("<tr>");
                stringBuild.AppendFormat("<td>{0}</td>", book.ISBN);
                stringBuild.AppendFormat("<td>{0}</td>", book.Title);
                stringBuild.AppendFormat("<td>{0}</td>", string.Join(", ", book.Authors));
                stringBuild.AppendFormat("<td>{0}</td>", book.Category);
                stringBuild.AppendFormat("<td>{0}</td>", book.Year);
                stringBuild.AppendFormat("<td>{0}</td>", book.Price);
                stringBuild.Append("</tr>");
            }

            stringBuild.Append("</table></body></html>");

            return Content(stringBuild.ToString(), "text/html");
        }

        [HttpPost]
        public IActionResult AddBook([FromBody] Book book)
        {
            if (books.Any(b => b.ISBN == book.ISBN))
            {
                return BadRequest("A book with the same ISBN already exists.");
            }
            books.Add(book);
            SaveBooksToXml(_xmlFilePath, books);
            return CreatedAtAction(nameof(GetBookByISBN), new { isbn = book.ISBN }, book);
        }

        [HttpPut("{isbn}")]
        public IActionResult UpdateBook(string isbn, [FromBody] Book updatedBook)
        {
            var existingBook = books.FirstOrDefault(b => b.ISBN == isbn);
            if (existingBook == null)
                return NotFound();

            if (books.Any(b => b.ISBN == updatedBook.ISBN && b.ISBN != isbn))
            {
                return BadRequest("A book with the same ISBN already exists.");
            }

            existingBook.Title = updatedBook.Title;
            existingBook.Authors = updatedBook.Authors;
            existingBook.Category = updatedBook.Category;
            existingBook.Year = updatedBook.Year;
            existingBook.Price = updatedBook.Price;

            SaveBooksToXml(_xmlFilePath, books);

            return NoContent();
        }

        [HttpDelete("{isbn}")]
        public IActionResult DeleteBook(string isbn)
        {
            var book = books.FirstOrDefault(b => b.ISBN == isbn);
            if (book == null)
                return NotFound();

            books.Remove(book);
            SaveBooksToXml(_xmlFilePath, books);

            return NoContent();
        }

        private static List<Book> LoadBooksFromXml(string filePath)
        {
            var xdoc = XDocument.Load(filePath);
            return xdoc.Root.Elements("book").Select(x => new Book
            {
                ISBN = x.Element("isbn").Value,
                Title = x.Element("title").Value,
                Authors = x.Elements("author").Select(a => a.Value).ToList(),
                Category = x.Attribute("category").Value,
                Year = int.Parse(x.Element("year").Value),
                Price = double.Parse(x.Element("price").Value)
            }).ToList();
        }


        private static void SaveBooksToXml(string filePath, List<Book> books)
        {
            var xdoc = new XDocument(
                new XElement("bookstore",
                    books.Select(book =>
                        new XElement("book",
                            new XAttribute("category", book.Category),
                            new XElement("isbn", book.ISBN),
                            new XElement("title", book.Title),
                            book.Authors.Select(author => new XElement("author", author)),
                            new XElement("year", book.Year),
                            new XElement("price", book.Price)
                        )
                    )
                )
            );
            xdoc.Save(filePath);
        }
    }

    public class Book
    {
        public string ISBN { get; set; }
        public string Title { get; set; }
        public List<string> Authors { get; set; }
        public string Category { get; set; }
        public int Year { get; set; }
        public double Price { get; set; }
    }
}
