﻿using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JabberBCIT.Models;
using System.Data;
using System.Data.SqlClient;

namespace JabberBCIT.Controllers
{
    //[Authorize] 
    public class ForumController : Controller
    {
        ChitterDbContext db = ChitterDbContext.Create;

        // GET: Forum
        public ActionResult Index(string tag = "Global")
        {
            if (db.Subforums.Any(x => x.Name == tag))
            {
                var listPostViewModel = new List<PostViewModel>();
                foreach (var p in db.ForumPosts.Where(p => p.Subforum.Name == tag))
                {
                    listPostViewModel.Add(new PostViewModel()
                    {
                        post = p,
                        PostTimestamp = p.PostTimestamp,
                        votes = p.ForumPostsVotes.Sum(x => x.Value)
                    });
                }
                ViewBag.ForumTitle = tag;

                // DO YOUR SORTING IN FOLLOWING METHOD//
                listPostViewModel.Sort((post1, post2) => sortFunction(post1, post2));

                return View(listPostViewModel);
            }
            return new EmptyResult();
        }
		
		/// <summary>
        /// Display create subforum view.
        /// </summary>
        /// <returns></returns>
        public ActionResult CreateSubForum()
        {
            return View();
        }
        /// <summary>
        /// Creates subforum and adds to database.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CreateSubForum(CreateSubForumViewModel model)
        {
            //if modelstate is valid
            if (ModelState.IsValid)
            {
                //create subforum off model
                var subforum = new Subforum
                {
                    Name = model.Name
                };

                //get all subforums in db
                var currentsubforums = db.Subforums.ToList();
                // the subforum id we are going to assign
                int newId = 1;
                //loop through subforums
                foreach (Subforum s in currentsubforums)
                {
                    ++newId;
                    //if existing subforum name equals subforum name to be inserted
                    if (s.Name == subforum.Name)
                    {
                        //display error
                        ViewBag.CreateSubForumError = "Subforum name already exists";
                        //return view with error messages
                        return View(model);
                    }
                }
                //attempt to insert into db
                try
                {
                    subforum.SubforumID = newId;
                    db.Subforums.Add(subforum);
                    db.SaveChanges();
                }
                catch
                {
                    //return white screen
                    return new EmptyResult();
                }
                //redirect subforum view
                return RedirectToAction("Index", new { tag = subforum.Name });
            }
            //if modelstate failed, return view with error messages
            return View(model);
        }
		
		

        public int sortFunction(PostViewModel p1, PostViewModel p2)
        {
            int compare = p2.votes.CompareTo(p1.votes);
            if (compare == 0)
            {
                return p2.PostTimestamp.CompareTo(p1.PostTimestamp);
            }
            return compare;
        }

        public ActionResult CreatePost()
        {
            return View();
        }
        [HttpPost]
        public ActionResult CreatePost(ForumPost post, string tag)
        {
            try
            {
                post.UserID = User.Identity.GetUserId();
                post.PostTimestamp = DateTime.Now;
                post.Subforum = db.Subforums.Where(x => x.Name == tag).FirstOrDefault();
                db.ForumPosts.Add(post);
                db.SaveChanges();
            }
            catch
            {
                return new EmptyResult();
            }
            return RedirectToAction(post.Subforum.Name, new { id = post.PostID });
        }

        public ActionResult DeletePost(long id)
        {
            return View(db.ForumPosts.Find(id));
        }

        [HttpPost]
        public ActionResult DeletePost(string tag, long id)
        {
            DataTable dtNames = new DataTable();
            string sqlQuery = "delete from ForumPosts where PostID = '" + id + "'";
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ChitterContext"].ConnectionString;
            try
            {
                SqlConnection conn = new SqlConnection(connectionString);
                SqlDataAdapter da = new SqlDataAdapter(sqlQuery, conn);
                da.Fill(dtNames);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            /*
            try
            {
                db.ForumPosts.Remove(db.ForumPosts.Find(id));
                db.SaveChanges();
            }
            catch
            {
                return new EmptyResult();
            }
            */
            return RedirectToAction( tag , "Forum");
        }

        public ActionResult CreateComment()
        {
            return View();
        }
        [HttpPost]
        public ActionResult CreateComment(Comment comment, long? commentID, long id)
        {
            try
            {
                comment.UserID = User.Identity.GetUserId();
                comment.PostTimestamp = DateTime.Now;
                comment.PostID = id;
                comment.ParentCommentID = commentID;
                db.Comments.Add(comment);
                db.SaveChanges();
            }
            catch
            {
                return new EmptyResult();
            }
            return RedirectToAction("ViewThread");
        }

        public ActionResult DeleteComment(long? commentID)
        {
            return View(db.Comments.Find(commentID));
        }

        [HttpPost]
        public ActionResult DeleteComment(long? commentID, long id)
        {
            
            try
            {
                db.Comments.Find(commentID).Hidden = 1;
                db.SaveChanges();
            }
            catch
            {
                return new EmptyResult();
            }
            return RedirectToAction("ViewThread");
        }

        public ActionResult ViewThread(long id)
        {
            if (db.ForumPosts.Any(x => x.PostID == id))
            {
                PostViewModel viewModel = new PostViewModel();
                viewModel.post = db.ForumPosts.Find(id);
                viewModel.votes = viewModel.post.ForumPostsVotes.Sum(x => x.Value);
                viewModel.childComments = getCommentTree(id);
                
                return View(viewModel);
            }
            return new EmptyResult();
        }

        List<CommentViewModel> getCommentTree(long basePostID)
        {
            List<CommentViewModel> model = new List<CommentViewModel>();
            
            // create commentviewmodels for every comment in this thread
            foreach (var comment in db.Comments.Where(x => x.PostID == basePostID).ToList())
            {
                model.Add(new CommentViewModel()
                {
                    votes = comment.CommentsVotes.Sum(x => x.Value),
                    comment = comment,
                    childComments = new List<CommentViewModel>(),
                });
            }

            // DO YOUR SORTING HERE //
            model.Sort((comment1, comment2) => comment2.votes.CompareTo(comment1.votes));
            // DO YOUR SORTING HERE //

            model.ForEach(i => i.childComments = model.Where(ch => ch.comment.ParentCommentID == i.comment.CommentID).ToList());
            return model.Where(x => x.comment.ParentCommentID == null).ToList();
        }
        
        public void VoteComment(long id, short value)
        {
            if (value == 1 || value == -1)
            {
                if (db.Comments.Any(x => x.CommentID == id))
                {
                    var oldVote = db.CommentsVotes.Find(id, User.Identity.GetUserId());
                    if (oldVote != null)
                    {
                        oldVote.Value = value;
                    }
                    else db.CommentsVotes.Add(new CommentsVote()
                    {
                        UserID = User.Identity.GetUserId(),
                        CommentID = id,
                        Value = value
                    });
                    db.SaveChanges();
                }
            }
        }

        public void VotePost(long id, short value)
        {
            if (value == 1 || value == -1)
            {
                if (db.ForumPosts.Any(x => x.PostID == id))
                {
                    var oldVote = db.ForumPostsVotes.Find(User.Identity.GetUserId(), id);
                    if (oldVote != null)
                    {
                        oldVote.Value = value;
                    }
                    else db.ForumPostsVotes.Add(new ForumPostsVote()
                    {
                        UserID = User.Identity.GetUserId(),
                        PostID = id,
                        Value = value
                    });
                    db.SaveChanges();
                }
            }
        }

        [ChildActionOnly]
        public ActionResult SidebarPartial()
        {
            return PartialView(db.Subforums.ToList());
        }
    }
}