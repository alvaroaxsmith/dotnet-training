using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ServidorHttp
{
    private TcpListener Controlador { get; set; }

    private int Porta { get; set; }

    private int QtdeRequests { get; set; }

    public string HtmlExemplo { get; set; }

    private SortedList<string, string> TiposMime { get; set; }
    private SortedList<string, string> DiretoriosHosts { get; set; }

    public ServidorHttp(int porta = 8080)
    {
        this.Porta = porta;
        this.CriarHtmlExemplo();
        this.PopularTiposMIME();
        this.PopularDiretoriosHosts();
        try
        {
            this.Controlador = new TcpListener(IPAddress.Parse("127.0.0.1"), this.Porta);
            this.Controlador.Start();
            Console.WriteLine($"Servidor HTTP está rodando na porta {this.Porta}.");
            Console.WriteLine($"Para acessar, digite no navegador: http://localhost:{this.Porta}.");
            Task servidorHttpTask = Task.Run(() => AguardarRequests());
            servidorHttpTask.GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao iniciar servidor na porta {this.Porta}:\n{e.Message}");
        }
    }
    private async Task AguardarRequests()
    {
        while (true)
        {
            Socket conexao = await this.Controlador.AcceptSocketAsync();
            this.QtdeRequests++;
            Task task = Task.Run(() => ProcessarRequest(conexao, this.QtdeRequests));
        }
    }
    private void ProcessarRequest(Socket conexao, int numeroRequest)
    {
        Console.WriteLine($"Processando request #{numeroRequest}...\n");
        if (conexao.Connected)
        {
            byte[] bytesRequisicao = new byte[1024];
            conexao.Receive(bytesRequisicao, bytesRequisicao.Length, 0);
            string textoRequisicao = Encoding.UTF8.GetString(bytesRequisicao)
                .Replace((char)0, ' ').Trim();
            if (textoRequisicao.Length > 0)
            {
                Console.WriteLine($"\n{textoRequisicao}\n");

                string[] linhas = textoRequisicao.Split("\r\n");
                int iPrimeiroEspaco = linhas[0].IndexOf(' ');
                int iSegundoEspaco = linhas[0].LastIndexOf(' ');
                string metodoHttp = linhas[0].Substring(0, iPrimeiroEspaco);
                string recursoBuscado = linhas[0].Substring(iPrimeiroEspaco + 1, iSegundoEspaco - iPrimeiroEspaco - 1);
                if (recursoBuscado == "/") recursoBuscado = "/index.html";
                recursoBuscado = recursoBuscado.Split("?")[0];
                string versaoHttp = linhas[0].Substring(iSegundoEspaco + 1);
                iPrimeiroEspaco = linhas[1].IndexOf(' ');
                
                string nomeHost = linhas[1].Substring(iPrimeiroEspaco + 1);

                byte[] bytesCabecalho = null;
                byte[] bytesConteudo = null;
                FileInfo fiArquivo = new FileInfo(ObterCaminhoFisicoArquivo(nomeHost, recursoBuscado));
                if (fiArquivo.Exists)
                {
                    if (TiposMime.ContainsKey(fiArquivo.Extension.ToLower()))
                    {
                        bytesConteudo = File.ReadAllBytes(fiArquivo.FullName);
                        string tipoMime = TiposMime[fiArquivo.Extension.ToLower()];
                        bytesCabecalho = GerarCabecalho(versaoHttp, tipoMime, "200", bytesConteudo.Length);
                    }
                    else
                    {
                        bytesConteudo = Encoding.UTF8.GetBytes("<h1>Erro 415 - Tipo de arquivo não suportado,</h1>");
                        bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf-8", "415", bytesConteudo.Length);
                    }

                }
                else
                {
                    bytesConteudo = Encoding.UTF8.GetBytes("<h1>Erro 404 - Arquivo Não Encontrado</h1>");
                    bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf-8", "404", bytesConteudo.Length);
                }
                int bytesEnviados = conexao.Send(bytesCabecalho, bytesCabecalho.Length, 0);
                bytesEnviados += conexao.Send(bytesConteudo, bytesConteudo.Length, 0);
                conexao.Close();
                Console.WriteLine($"\n{bytesEnviados} bytes enviados em resposta à requisição #{numeroRequest}.");
            }
            Console.WriteLine($"\nRequest{numeroRequest} finalizado.");
        }}
        public byte[] GerarCabecalho(string versaoHttp, string tiposMime, string codigoHttp, int qtdeBytes = 0)
        {
            StringBuilder texto = new StringBuilder();
            texto.Append($"{versaoHttp} {codigoHttp}{Environment.NewLine}");
            texto.Append($"Server: Servidor Http Simples 1.0{Environment.NewLine}");
            texto.Append($"Content-Type: {tiposMime}{Environment.NewLine}");
            texto.Append($"Content-Length: {qtdeBytes}{Environment.NewLine}{Environment.NewLine}");
            return Encoding.UTF8.GetBytes(texto.ToString());
        }

        private void CriarHtmlExemplo()
        {
            StringBuilder html = new StringBuilder();
            html.Append("<!DOCTYPE html><html lang=\"pt-br\"><head><meta charset=\"UTF-8\">");
            html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.Append("<title>Página Estática</title></head><body>");
            html.Append("<h1>Página Estática</h1></body></html>");
            this.HtmlExemplo = html.ToString();
        }

        private void PopularTiposMIME()
        {
            this.TiposMime = new SortedList<string, string>();
            this.TiposMime.Add(".html", "text/html;charset=utf-8");
            this.TiposMime.Add(".htm", "text/html;charset=utf-8");
            this.TiposMime.Add(".css", "text/css");
            this.TiposMime.Add(".js", "text/javascript");
            this.TiposMime.Add(".png", "image/png");
            this.TiposMime.Add(".jpg", "image/jpeg");
            this.TiposMime.Add(".gif", "image/gif");
            this.TiposMime.Add(".svg", "image/svg+xml");
            this.TiposMime.Add(".webp", "image/webp");
            this.TiposMime.Add(".ico", "image/ico");
            this.TiposMime.Add(".woff", "font/woff");
            this.TiposMime.Add(".woff2", "font/woff2");
        }

        private void PopularDiretoriosHosts()
        {
            this.DiretoriosHosts = new SortedList<string, string>();
            this.DiretoriosHosts.Add("localhost", "C:\\Users\\alvar\\OneDrive\\Documents\\workspace\\servidorhttpsimples\\www\\localhost");
            this.DiretoriosHosts.Add("maroquio.com", "C:\\Users\\alvar\\OneDrive\\Documents\\workspace\\servidorhttpsimples\\www\\maroquio.com");
            this.DiretoriosHosts.Add("dogefoods.herokuapp.com", "C:\\Users\\alvar\\OneDrive\\Documents\\workspace\\doge-foods");
        }
        
        public string ObterCaminhoFisicoArquivo(string host, string arquivo)
        {
            string diretorio = this.DiretoriosHosts[host.Split(":")[0]];
            string caminhoArquivo = diretorio + arquivo.Replace("/", "\\");
            return caminhoArquivo;
        }
    
        
    }