using Microsoft.AspNetCore.Mvc;
using SeminarHelperServer.Models;
using StackExchange.Redis;
using SeminarHelperServer.DataObject;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;

namespace SeminarHelperServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SeminarController : ControllerBase
    {
        /*
         * 1. Создание семинара. Семинар создаётся здесь и сейчас с заданным количеством текстовых полей и первым свободным id. Проверяется пароль учителя.
         * 2. Подключение к семинару. Создаётся ученик с заданным именем и фамилией и подключается к семинару с заданным id. Создается пустой Redis список его текстовых полей.
         * 3. Сохранение изменений в текстовом поле. Ученик с таким-то id в таком-то текстовом поле сохраняет текст (запросы посылаются автоматически).
         * 4. Обновление позиции ученика в документе. Посылается каждую секунду.
         * 5. Учитель просматривает текстовое поле ученика. По id ученика показываются результаты ученика. 
         * 6. Учитель просматривает позицию всех учеников на поле.
         */

        private static ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("127.0.0.1:6379");
        private static IDatabase db = redis.GetDatabase();
        static public List<int> freeIds = new List<int>();
        static public List<Seminar> seminars = new List<Seminar>();

        public SeminarController()
        {
            db = redis.GetDatabase();
        }

        [HttpGet("test")]
        public IActionResult test()
        {
            return Ok("Test");
        }

        ~SeminarController()
        {
            redis.Close();
        }

        [HttpPost("NewSeminar")]
        async public Task<IActionResult> RegisterNewSeminar()
        {
            Seminar newSeminar = new Seminar();

            int i = 0;

            while (true)
            {
                if(!freeIds.Contains(i))
                {
                    freeIds.Add(i);
                    newSeminar.Id = i;
                    break;
                }

                i += 1;    
            }

            newSeminar.textboxes = 1000;
            newSeminar.created = DateTime.Now;
            newSeminar.students = new List<Student>();

            seminars.Add(newSeminar);

            return Ok(newSeminar);
        }

        [HttpPost("NewStudent/{surname}/{name}/{seminarID}")]
        async public Task<IActionResult> RegisterNewStudent (int seminarID, string name, string surname)
        {
            Student newStudent = new Student();

            newStudent.Name = name;
            newStudent.Surname = surname;
            newStudent.Position = 0;

            for(int i = 0; i < seminars.Count; i++)
            {
                if (seminars[i].Id == seminarID)
                {
                    newStudent.Id = seminars[i].students.Count;
                    newStudent.SeminarID = seminarID;
                    seminars[i].students.Add(newStudent);

                    for (int j = 0; j < seminars[i].textboxes; j++)
                    {
                        db.ListLeftPush(newStudent.Id.ToString(), "");
                    }
                }
            }

            return Ok(newStudent);
        }

        [HttpPut("students/{seminarId}/{studentId}")]
        async public Task<IActionResult> TextboxUpdate (int seminarId, int studentId, [FromBody] List<string> arrayJSON)
        {
            Seminar thisSeminar = seminars.Find(seminar => seminar.Id == seminarId);

            System.Diagnostics.Debug.WriteLine(arrayJSON);

            int min = Math.Min(arrayJSON.Count, thisSeminar.textboxes);

            for (int i = 0; i < min; i++)
            {
                db.ListSetByIndex(studentId.ToString(), i, arrayJSON[i]);
            }

            System.Diagnostics.Debug.WriteLine("Sent");

            return Ok();
        }

        [HttpPut("students/{seminarId}/{studentId}/{scrolled}/{max}")]
        async public Task<IActionResult> PositionUpdate (int seminarId, int studentId, double scrolled, double max)
        {
            if(scrolled == -1)
            {
                return Ok();
            }

            Seminar thisSeminar = seminars.Find(seminar => seminar.Id == seminarId);
            Student thisStudent = thisSeminar.students.Find(student => student.Id == studentId);

            thisStudent.Position = scrolled/max;

            System.Diagnostics.Debug.WriteLine("Sent");
            return Ok();
        }

        [HttpGet("teacher/getStudent/{studentId}")]
        async public Task<IActionResult> GetStudentText (int studentId)
        {
            List<TextInfo> allTexts = new List<TextInfo>();

            for (int i = 1; i < 1000; i++)
            {
                string thisText = db.ListGetByIndex(studentId.ToString(), i);

                if (thisText != null && thisText.Length > 0)
                {
                    TextInfo textInfo = new TextInfo();
                    textInfo.Text = thisText;
                    textInfo.Number = i;

                    allTexts.Add(textInfo);
                }
            }

            return Ok(allTexts);
        }

        [HttpGet("teacher/getSeminar/{seminarId}")]
        async public Task<IActionResult> GetStudentPositions (int seminarId)
        {
            Seminar thisSeminar = seminars.Find(seminar => seminarId == seminar.Id);

            List<Student> result = new List<Student>();

            for (int i = 0; i < thisSeminar.students.Count; i++)
            {
                result.Add(thisSeminar.students[i]);
            }

            return Ok(result);
        }

        [HttpGet("GetAllSeminars")]
        async public Task<IActionResult> GetAllSeminars()
        {
            return Ok(seminars);
        }
    }
}
