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

            var type = typeof(T);

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
                }
                else if(!value1.Equals(value2))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
