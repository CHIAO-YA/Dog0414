namespace Dog.Migrations
{
    using Dog.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Data.SqlTypes;
    using System.Linq;
    using System.IO;
    using System.Diagnostics;
    using System.Numerics;
    using System.Data.Entity.Validation;

    internal sealed class Configuration : DbMigrationsConfiguration<Dog.Models.Model1>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(Dog.Models.Model1 context)
        {
            var planPath = @"C:\Users\acer\Downloads\plans.json";
            if (!File.Exists(planPath))
            {
                Console.WriteLine("❌ JSON file not found: " + planPath);
                return;
            }

            // 反序列化：支援多筆使用者（請確保 JSON 格式是陣列）
            var planList = JsonConvert.DeserializeObject<List<Plan>>(File.ReadAllText(planPath));

            foreach (var plan in planList)
            {
                if (plan != null)
                {

                    plan.PlanID = plan.PlanID;
                    plan.PlanName = plan.PlanName;
                    plan.Liter = plan.Liter;
                    plan.Price = plan.Price;
                    plan.PlanKG = plan.PlanKG;
                    plan.PlanDescription = plan.PlanDescription;
                    plan.PlanPeople = plan.PlanPeople;
                    context.Plans.AddOrUpdate(plan); // 或 Add(user)
                }
            }
            try
            {
                context.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var entityValidationError in ex.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        entityValidationError.Entry.Entity.GetType().Name, entityValidationError.Entry.State);
                    foreach (var validationError in entityValidationError.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            validationError.PropertyName, validationError.ErrorMessage);
                    }
                }
            }

        }
    }
}
