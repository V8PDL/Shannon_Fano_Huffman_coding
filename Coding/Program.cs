using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Coding
{
    class Program
    {
        class Text_for_coding
        {
                public string Original_Text { get; }        // Текст хранится в оригинальном виде, поэтому не стоит загружать
                                                            // очень большие файлы, оперативной памяти не всегда хватает
            public List<(char, int)> Frequency { get; }     // Количество (Item2) каждого символа (Item1)
            public Dictionary<char, string> Uni_dict { get; }   // Словарь для равномерного кода
            public Dictionary<char, string> Huffman_dict { get; }   // Для Хаффмана
            public Dictionary<char, string> Shannon_Fano_dict { get; }  // Для Шеннона-Фано
            public int Length { get; }  // Количество учитываемых символов, то есть без разделителей вроде табуляции и абзацев
            public double Entropy { get; }              // Энтропия
            private double Shannon_Fano_Avg_Length;     // Средняя длина кода для Шеннона-Фано
            private double Huffman_Avg_Length;          // Для Хаффмана
            public Text_for_coding(string path)         // Конструктор, инициализирует пустые словари и считывает текст
            {
                Frequency = new List<(char, int)>();
                Uni_dict = new Dictionary<char, string>();
                Huffman_dict = new Dictionary<char, string>();
                Shannon_Fano_dict = new Dictionary<char, string>();

                string text = new StreamReader(path).ReadToEnd();

                Original_Text = text;
                text = text.Replace("\n", " ").Replace("\r", "").Replace("\t", "");
                Length = text.Length;
                while (text.Length > 0)                             // Сразу получаем таблицу вероятностей (Frequency)
                                                                    // Для чистоты, может, лучше даже выделить в отдельную функцию
                {
                    Frequency.Add((text[0], text.Count(c => c.Equals(text[0]))));
                    text = text.Replace(text[0].ToString(), "");
                }
                Frequency.Sort((p, q) => q.Item2.CompareTo(p.Item2));
                Entropy = 0;
                foreach (var pair in Frequency)                 // Рассчет энтропии по стандартной формуле 
                {
                    double q = (double)pair.Item2 / (double)Length;
                    Entropy -= q * Math.Log(q, 2);
                }
            }                                  
            public double Set_Uni_dict()                // Равномерный код, всё просто - всем символам присваиваются номера
                                                        // и переводятся в двоичный вид
            {
                int length = (int)Math.Log(Frequency.Count, 2);
                length += Math.Log(Frequency.Count, 2) == length ? 0 : 1;

                for (int i = 0; i < Frequency.Count; i++)
                    Uni_dict.Add(Frequency[i].Item1, Convert.ToString(i, 2).PadLeft(length, '0'));
                return (double)length;
            }                                          
            public double Set_Huffman_dict()            // Для Хаффмана алгоритм несколько сложнее.
            {
                List<(List<(char, string)>, int)> column = new List<(List<(char, string)>, int)>();
                foreach (var f in Frequency)
                    column.Add((new List<(char, string)> { (f.Item1, "") }, f.Item2));  // Инициализация столбца таблицы имеющимися символами,
                                                                                        // как обычно при начале использования метода Хаффмана.

                for (int i = Frequency.Count - 1; i > 0; i--)
                {
                    for (int j = 0; j < column[i].Item1.Count; j++)
                        column[i].Item1[j] = (column[i].Item1[j].Item1, column[i].Item1[j].Item2 + "0");
                    // Танец с бубном из-за сложности типа данных. Суть такова - в каждой ячейке столбца хранится список принадлежащих ему символов.
                    // Изначально - это по одному символу в порядке убывания вероятности. Далее последняя ячейка "заносится" в предпоследнюю:
                    // в результате количество ячеек столбца уменьшается на 1, и она начинает включать в себя оба набора символов (для первого шага - два самых редких).
                    // Причем перед этим кодам последней ячейки прибавляется "0", а предпоследней - "1". Таким образом самый длинный код будут иметь самые редкие символы. 

                    for (int j = 0; j < column[i - 1].Item1.Count; j++)
                        column[i - 1].Item1[j] = (column[i - 1].Item1[j].Item1, column[i - 1].Item1[j].Item2 + "1");

                    column[i - 1].Item1.AddRange(column[i].Item1);              // Добавляется массив из последней ячейки в предпоследнюю
                    column[i - 1] = (column[i - 1].Item1, column[i - 1].Item2 + column[i].Item2); 
                        // Что важно - их вероятности складываются, так много редких символов накапливаются, как снежный ком. 
                    column.Sort((p, q) => q.Item2.CompareTo(p.Item2));  // Обязательно сортируем, чтобы внизу снова оказались самые редкие. 
                        // Вообще-то можно и просто искать самый редкий, без сортировки. Но так выходит нагляднее, если выводить всю таблицу преобразований.
                    column.Remove(column[i]);   // Удаляем последнюю ячейку, включенную в предпоследнюю. 
                }
                foreach (var v in column[0].Item1)
                    Huffman_dict.Add(v.Item1, v.Item2);
                // В итоге получился массив для всех символов, собранных в одну оставшуюся ячейку столбца

                Huffman_Avg_Length = 0;     // Нахождение средней длины кода. возвращаемое значение.
                foreach (var f in Frequency)
                {
                    double q = (double)f.Item2 / Length;
                    Huffman_Avg_Length += q * (double)Huffman_dict[f.Item1].Length;
                }
                return Huffman_Avg_Length;
            }
            public double Set_Shannon_Fano_dict()
            {
                foreach (var t in Frequency)
                    Shannon_Fano_dict.Add(t.Item1, ""); // Инициализация. Рабочая рекурсивная функция дальше.

                Shannon_Fano(0, Frequency.Count, Length);   // Вот она. Сама функция описана в конце класса.

                Shannon_Fano_Avg_Length = 0;    // Средняя длина кода, то же самое. 
                foreach (var f in Frequency)
                {
                    double q = (double)f.Item2 / Length;
                    Shannon_Fano_Avg_Length += q * (double)Shannon_Fano_dict[f.Item1].Length;
                }
                return Shannon_Fano_Avg_Length;
            }                                        
            public string Encoding(Dictionary<char, string> dictionary, string Text) // Основная функция кодирования.
            {
                string result = "";
                int Unexpected_Symbol = 0;     // Если есть символ, которого нет в словаре, то нужно предупредить - что-то пошло не по плану.
                foreach (var c in Text)
                    if (dictionary.ContainsKey(c))
                        result += dictionary[c];
                    else
                    {
                        result += c.ToString();
                        if (!string.IsNullOrWhiteSpace(c.ToString()))   // Разделители вроде табуляции должны присутствовать, на них не нужно реагировать.
                            Unexpected_Symbol++;
                    }
                Console.WriteLine($"{result.Length - Unexpected_Symbol} of '0' and '1' was wrote successfully");
                if (Unexpected_Symbol != 0)
                    Console.WriteLine($"{Unexpected_Symbol} symbols was not encoded!\nIf you will let them weight {Uni_dict.Max(k => k.Value.Length) + 1}, your code length will be {result.Length + Unexpected_Symbol * (Uni_dict.Max(k => k.Value.Length))}");
                return result;
            } 
            public string Encoding(Dictionary<char, string> dictionary) => Encoding(dictionary, Original_Text); // Для удобства
            public string Encoding(int dict_number, string text) // Аналогично.
            {
                switch (dict_number)
                {
                    case 1: return Encoding(Uni_dict, text);
                    case 2: return Encoding(Huffman_dict, text);
                    case 3: return Encoding(Shannon_Fano_dict, text);
                    default:
                        return "Incorrect number; 1, 2, 3 allowed";
                }
            }   
            public string Encoding(int dict_number) => Encoding(dict_number, Original_Text);    // На всякий случай.
            private void Shannon_Fano(int start, int end, int sum)      // Стартовая позиция в таблице, конечная и сумма вероятностей этого промежутка
            {
                // Суть метода Шеннона-Фано в последовательном делении сортированной таблицы на две части, причем с как можно более совпадающими вероятностями,
                // то есть они должны стремиться к половине. Символам, оставшимся в верхней половине, присваивается "0", в нижней - "1". 
                if (end - start == 1)       // Условие выхода из рекурсии - если остался один символ, то делить нечего.
                    return;
                int count = 0;      // Накопитель для суммы
                int new_end = 0;        // Отметка для середины
                bool finded = false;    // Флаг, означающий, что середина таблицы (по сумме вероятности) найдена
                for (int i = start; i < end; i++)
                {
                    if (!finded)    // Ищем середину. 
                    {
                        if (count + Frequency[i].Item2 > sum / 2)
                        {
                            finded = true;      // Нашлась
                            new_end = i;
                            if (sum / 2 - count >= count + Frequency[i].Item2 - sum / 2)
                              // Возможны две ситуации - когда чтобы быть ближе к равным половинам нужно 
                              // взять дополнительную строку, и когда она ,была бы лишней.
                                count += Frequency[new_end++].Item2;
                        }
                        else
                            count += Frequency[i].Item2;
                        Shannon_Fano_dict[Frequency[i].Item1] += "0";
                    }
                    else
                        Shannon_Fano_dict[Frequency[i].Item1] += "1"; // Всей ставшейся таблице просто прибавляем "1"
                }
                Shannon_Fano(start, new_end, count);        // Для найденных границ запускаем тот же самый алгоритм, разделяя 
                Shannon_Fano(new_end, end, sum - count);    // таблицу на половины до тех пор, пока значения не будут заданы каждому символу. 
            }
            public void Show_Table(Dictionary<char, string> dictionary) // Всё просто,одно лишь форматирование, никакой логики.
            {
                string format = "\t| {0}:     | {1}\t| {2}\t|";
                Console.WriteLine("Coding table for uniform code:");
                Console.WriteLine("\t---------------------------------------------------------");
                Console.WriteLine("\t| Symbol | Probability\t\t| Code\t\t\t|");
                Console.WriteLine("\t---------------------------------------------------------");
                foreach (var pair in Frequency)
                {
                    double q = (double)pair.Item2 / (double)Length;
                    Console.WriteLine(string.Format(format, pair.Item1.ToString(), q.ToString(), dictionary[pair.Item1].PadRight(20, ' ')));
                }
                Console.WriteLine("\t---------------------------------------------------------");
                Console.WriteLine("Continue?");
                Console.ReadLine();
            }   
        }
        static void Main()
        {
            Text_for_coding[] texts = new Text_for_coding[2];

            Console.WriteLine("Enter names of 2 txt files (UTF-8)");
            do
            {
                try
                {           // Если предполагается, что ввод всегда безошибочен, конечно, можно упростить всю эту конструкцию до двух строк.
                    if (texts[0] == null)
                    {
                        Console.WriteLine("Enter first file's name (full path, if in different directories with .exe file):");
                        texts[0] = new Text_for_coding(Console.ReadLine());
                    }
                    if (texts[1] == null)
                    {
                        Console.WriteLine("Enter second file's name:");
                        texts[1] = new Text_for_coding(Console.ReadLine());
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Can't read this file, enter name another time, please");
                    continue;
                }
                break;
            }
            while (texts[0] == null || texts[1] == null);


            for (int i = 0; i < texts.Length; i++)
            {
                Console.WriteLine("Entropy of the first text is " + texts[i].Entropy);
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Uniform code for text number " + (i + 1).ToString() + " have length " + texts[i].Set_Uni_dict().ToString() + "\n");
                texts[i].Show_Table(texts[i].Uni_dict);
                Console.WriteLine("Shannon-Fano code for text number " + (i + 1).ToString() + " have average length " + texts[i].Set_Shannon_Fano_dict().ToString());
                texts[i].Show_Table(texts[i].Shannon_Fano_dict);
                Console.WriteLine("Huffman code for text number " + (i + 1).ToString() + " have length " + texts[i].Set_Huffman_dict().ToString());
                texts[i].Show_Table(texts[i].Huffman_dict);
                Console.WriteLine("Encoded texts will be saved to .exe's folder");
                Console.WriteLine("----------------------------------------");
            }

            
            for (int i = 0; i < texts.Length; i++)
            {
                using (StreamWriter sw = new StreamWriter($"text{(i + 1).ToString()} uniform code.txt"))
                {
                    Console.WriteLine($"Writing \"text{(i + 1).ToString()} uniform code.txt\"");
                    sw.WriteLine(texts[i].Encoding(texts[i].Uni_dict));
                }
                using (StreamWriter sw = new StreamWriter($"text{(i + 1).ToString()} Shannon-Fano code.txt"))
                {
                    Console.WriteLine($"Writing \"text{(i + 1).ToString()} Shannon-Fano code.txt\"");
                    sw.WriteLine(texts[i].Encoding(texts[i].Shannon_Fano_dict));
                }
                using (StreamWriter sw = new StreamWriter($"text{(i + 1).ToString()} Huffman code.txt"))
                {
                    Console.WriteLine($"Writing \"text{(i + 1).ToString()} Huffman code.txt\"");
                    sw.WriteLine(texts[i].Encoding(texts[i].Huffman_dict));
                }

                Console.WriteLine("Writing to \"text" + (i + 1).ToString() + " by other Shannon-Fano code.txt\"");
                using (StreamWriter sw = new StreamWriter("text" + (i + 1).ToString() + " by other Shannon-Fano code.txt"))
                    sw.WriteLine(texts[i].Encoding(texts[(1 + i) % 2].Shannon_Fano_dict));
                Console.WriteLine("Writing to \"text" + (i + 1).ToString() + " by other Huffman code.txt\"");
                using (StreamWriter sw = new StreamWriter("text" + (i + 1).ToString() + " by other Huffman code.txt"))
                    sw.WriteLine(texts[i].Encoding(texts[(1 + i) % 2].Huffman_dict));
                Console.WriteLine("----------------------------------------");
            }
            Console.WriteLine("That's all!\nEnter to exit");
            Console.ReadLine();
        }
    }
}
