using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.OData;
using Simple.OData.ProductService.Models;

namespace Simple.OData.ProductService.Controllers
{
    public class WorkTaskModelsController : ODataController
    {
        private ProductServiceContext db = new ProductServiceContext();


        // GET odata/WorkTaskModels
        [EnableQuery]
        public IQueryable<WorkTaskModel> GetWorkTaskModels()
        {
            return db.WorkTaskModels;
        }

        // GET odata/WorkTaskModels(5)
        [EnableQuery]
        public SingleResult<WorkTaskModel> GetWorkTaskModel([FromODataUri] Guid key)
        {
            return SingleResult.Create(db.WorkTaskModels.Where(workTaskModel => workTaskModel.Id == key));
        }

        // PUT odata/WorkTaskModels(5)
        public IHttpActionResult Put([FromODataUri] Guid key, WorkTaskModel workTaskModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (key != workTaskModel.Id)
            {
                return BadRequest();
            }

            db.Entry(workTaskModel).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorkTaskModelExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Updated(workTaskModel);
        }

        // POST odata/WorkTaskModels
        public IHttpActionResult Post(WorkTaskModel workTaskModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.WorkTaskModels.Add(workTaskModel);
            db.SaveChanges();

            return Created(workTaskModel);
        }

        // PATCH odata/WorkTaskModels(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] Guid key, Delta<WorkTaskModel> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            WorkTaskModel workTaskModel = db.WorkTaskModels.Find(key);
            if (workTaskModel == null)
            {
                return NotFound();
            }

            patch.Patch(workTaskModel);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorkTaskModelExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Updated(workTaskModel);
        }

        // DELETE odata/WorkTaskModels(5)
        public IHttpActionResult Delete([FromODataUri] Guid key)
        {
            WorkTaskModel workTaskModel = db.WorkTaskModels.Find(key);
            if (workTaskModel == null)
            {
                return NotFound();
            }

            db.WorkTaskModels.Remove(workTaskModel);
            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool WorkTaskModelExists(Guid key)
        {
            return db.WorkTaskModels.Count(e => e.Id == key) > 0;
        }
         
    }
}