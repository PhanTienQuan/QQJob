using System.Collections;

namespace QQJob.Helper
{
    public static class ObjectComparer
    {
        public static bool AreEqual<T>(T obj1,T obj2)
        {
            if(obj1 == null || obj2 == null)
            {
                return obj1 == null && obj2 == null;
            }
            var type = obj1.GetType();

            // Get all readable properties of the object
            var properties = type.GetProperties()
                                 .Where(p => p.CanRead);

            foreach(var property in properties)
            {
                var value1 = property.GetValue(obj1);
                var value2 = property.GetValue(obj2);

                // Handle null values
                if(value1 == null || value2 == null)
                {
                    if(value1 != value2)
                    {
                        return false;
                    }
                    continue;
                }

                // Handle collections
                if(value1 is IEnumerable collection1 && value2 is IEnumerable collection2 && !(value1 is string) && !(value2 is string))
                {
                    if(!AreCollectionsEqual(collection1,collection2))
                    {
                        return false;
                    }
                    continue;
                }


                // Handle nested objects
                if(property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    if(!AreEqual(value1,value2))
                    {
                        return false;
                    }
                    continue;
                }

                // Compare scalar values
                if(!value1.Equals(value2))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreCollectionsEqual(IEnumerable col1,IEnumerable col2)
        {
            var list1 = col1.Cast<object>().ToList();
            var list2 = col2.Cast<object>().ToList();

            if(list1.Count != list2.Count)
            {
                return false;
            }

            for(int i = 0;i < list1.Count;i++)
            {
                if(!AreEqual(list1[i],list2[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
