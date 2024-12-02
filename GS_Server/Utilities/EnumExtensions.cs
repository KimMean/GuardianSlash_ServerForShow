using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GS_Server.Packets;

namespace GS_Server.Utilities
{
    public static class EnumExtensions
    {
        public static string ToCustomString(this Products product)
        {
            // 제품의 문자열 이름을 얻기 위해 ToString()을 호출
            FieldInfo field = product.GetType().GetField(product.ToString());

            if (field == null)
            {
                return product.ToString(); // 필드가 없으면 기본 ToString() 반환
            }

            // 해당 필드에 할당된 ProductStringAttribute를 가져옴
            ProductStringAttribute attribute = (ProductStringAttribute)Attribute.GetCustomAttribute(field, typeof(ProductStringAttribute));


            // 속성이 존재하면 속성 값을 반환하고, 없으면 기본 ToString() 값을 반환
            return attribute == null ? product.ToString() : attribute.Name;
        }
    }
}
