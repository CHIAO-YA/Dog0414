using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Dog.Models;

namespace Dog.Controllers
{
    public class UserController : ApiController
    {
        //Models.Model1 db = new Models.Model1();

        //[HttpGet]
        //[Route("User")]//取得所有使用者
        //public IHttpActionResult GetUser()
        //{
        //    var result = db.Users.ToList();
        //    return Ok(new
        //    {
        //        StatusCode = 200,
        //        status = "成功取得",
        //        result
        //    });
        //}

        //[HttpGet]
        //[Route("User/{id}")]//取得單筆使用者
        //public IHttpActionResult GetUser(int id)
        //{
        //    var user = db.Users.FirstOrDefault(u=>u.UsersID == id);
        //    var result = new
        //    {
        //        Status = HttpStatusCode.OK,
        //        msg = "成功取得",
        //        user
        //    };
        //    if (user == null)
        //    {
        //        return NotFound();
        //    }
        //    return Ok(result);
        //}

        //[HttpPost]
        //[Route("User")]//新增使用者
        //public IHttpActionResult CreateUser([FromBody] Users user)
        //{
        //    if(!ModelState.IsValid)//檢查請求
        //    {
        //        return BadRequest(ModelState);//請求無效，回傳400錯誤
        //    }
        //    var currentTimeUtc = DateTime.UtcNow;
        //    user.CreatedAt = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);
        //    db.Users.Add(user);
        //    db.SaveChanges();
        //    return Ok(new
        //    {
        //        StatusCode = 200,
        //        status = "新增成功",
        //        user
        //    });
        //}
        //[HttpPut]
        //[Route("User/{id}")]//修改使用者
        //public IHttpActionResult UpdateUser(int id, [FromBody] Users user)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }
        //    var currentUser = db.Users.FirstOrDefault(u => u.UsersID == id);
        //    if (currentUser == null)
        //    {
        //        return NotFound();
        //    }
        //    currentUser.Number = user.Number;
        //    currentUser.Roles = user.Roles;
        //    currentUser.Name = user.Name;
        //    currentUser.Phone = user.Phone;
        //    currentUser.LinePicUrl = user.LinePicUrl;
        //    currentUser.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        //    db.SaveChanges();
        //    return Ok(new
        //    {
        //        StatusCode = 200,
        //        status = "修改成功",
        //        currentUser
        //    });
        //}
        //[HttpDelete]
        //[Route("User/{id}")]//刪除使用者
        //public IHttpActionResult DeleteUser(int id)
        //{
        //    var user = db.Users.FirstOrDefault(u => u.UsersID == id);
        //    if (user == null)
        //    {
        //        return NotFound();
        //    }
        //    db.Users.Remove(user);
        //    db.SaveChanges();
        //    return Ok(new
        //    {
        //        StatusCode = 200,
        //        status = "刪除成功"
        //    });
        //}
    }
}
