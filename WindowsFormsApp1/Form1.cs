using McProtocol;
using McProtocol.Mitsubishi;
using MetroFramework.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


namespace WindowsFormsApp1
{
  public partial class Form1 : MetroFramework.Forms.MetroForm
  {
    // MQTT Setting 
    private IMqttClient client;
    private MqttClientOptions clientOptions;
    public delegate void _ShowMessageRT(string msg, string s);

    public Form1()
    {
      InitializeComponent();
      // Đưa con trỏ tại Ip khi bắt đầu chạy chương trình
      this.ActiveControl = textBox_ip1;
      textBox_ip1.Focus();
    }

    McProtocolTcp mcProtocolTcp;

    private async void button_connect_Click_1(object sender, EventArgs e)
    {
      int port = Convert.ToInt32(textBox_port.Text);
      string iP = textBox_ip1.Text + "." + textBox_ip2.Text + "." + textBox_ip3.Text + "." + textBox_ip4.Text;

      try
      {
        mcProtocolTcp = new McProtocolTcp(iP, port, McFrame.MC3E);
        await mcProtocolTcp.Open();

        MessageBox.Show("Connect Successful!");
        timer1.Start();
        timer_update_database.Start();
      }
      catch 
      {
        MessageBox.Show("Connect failed");
        this.Close();
      }
    }

    string[] id_register = {"D3000", "D3001", "D3002", "D3003", "D3004", "D3005", "D3006", "D3007", "D3008", "D3009" };
    int[] value_register = new int[10];
    string[] value_read_broker = new string[10];

    private void comboBox_read_SelectedIndexChanged(object sender, EventArgs e)
    {
      textBox_value_read.Text = value_read_broker[comboBox_read.SelectedIndex].ToString();
    }

    // status connect PLC
    private void timer1_Tick(object sender, EventArgs e)
    {
      button_status.Visible = !button_status.Visible;
    }

    private void timer_update_database_Tick(object sender, EventArgs e)
    {
      //Khi không kết nối PLC
      randomdata();
      updateData();
    }

    // Random data làm mẫu
    public async void randomdata()
    {
      Random rd = new Random();

      for (int i = 0; i < 10; i++)
      {
        value_register[i]=rd.Next(1,1000);
        try
        {
          var message = new MqttApplicationMessageBuilder()
              .WithTopic(id_register[i].Trim())
              .WithPayload(value_register[i].ToString())
              .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
              .WithRetainFlag()
              .Build();
          await client.PublishAsync(message, CancellationToken.None);
        }
        catch (Exception ex)
        {
          MessageBox.Show(ex.Message);
        }
      }
    }

    // MQTT
    private async void button1_Click(object sender, EventArgs e)
    {
      var BrokerAddress = textBox_broker.Text;
      int BrokerPort = Convert.ToInt32(textBox_port_broker.Text);
      // use a unique id as client id, each time we start the application
      var clientId = Guid.NewGuid().ToString();

      var factory = new MqttFactory();
      client = factory.CreateMqttClient();
      clientOptions = new MqttClientOptionsBuilder()
          .WithTcpServer(BrokerAddress, BrokerPort) // Port is optional
          .WithClientId(clientId)
          .Build();

      client.ConnectedAsync += Client_ConnectedAsync;
      client.ConnectingAsync += Client_ConnectingAsync;
      client.DisconnectedAsync += Client_DisconnectedAsync;
      client.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;

      await client.ConnectAsync(clientOptions, CancellationToken.None);

      timer_update_database.Start();
      timer1.Start();
      subcribe();
      updateData();  
    }
    private Task Client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
      // get payload
      string ReceivedMessage = Encoding.UTF8.GetString(arg.ApplicationMessage.Payload);

      // get topic name
      string TopicReceived = arg.ApplicationMessage.Topic;

      // Show message
      ShowMessageRT(ReceivedMessage, TopicReceived);

      return Task.CompletedTask;
    }

    // Disconnect
    private async Task Client_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
      label_status_mqtt.Text = "Disconnected";

      await Task.Delay(TimeSpan.FromSeconds(3));
      await client.ConnectAsync(clientOptions, CancellationToken.None); 
      label_status_mqtt.Text = "Reconnecting";

      await Task.CompletedTask;
    }

    // Connecting
    private async Task Client_ConnectingAsync(MqttClientConnectingEventArgs arg)
    {
      label_status_mqtt.Text = "Reconnecting ...";
      await Task.CompletedTask;
    }

    // Connected
    private async Task Client_ConnectedAsync(MqttClientConnectedEventArgs arg)
    {
      label_status_mqtt.Text = "Connected";

      this.Invoke((MethodInvoker)delegate
      {
        button_connect_broker.Enabled = false;
        textBox_broker.Enabled = false;
      });

      await Task.CompletedTask;
    }


    public void ShowMessageRT(String msg, String s)
    {
      if (InvokeRequired)
      {
        Invoke(new _ShowMessageRT(ShowMessageRT), new Object[] { msg, s });
        return;
      }

      switch (s)
      {
        case "D3000":
          value_read_broker[0] = msg;
          break;
        case "D3001":
          value_read_broker[1] = msg;
          break;
        case "D3002":
          value_read_broker[2] = msg;
          break;
        case "D3003":
          value_read_broker[3] = msg;
          break;
        case "D3004":
          value_read_broker[4] = msg;
          break;
        case "D3005":
          value_read_broker[5] = msg;
          break;
        case "D3006":
          value_read_broker[6] = msg;
          break;
        case "D3007":
          value_read_broker[7] = msg;
          break;
        case "D3008":
          value_read_broker[8] = msg;
          break;
        case "D3009":
          value_read_broker[9] = msg;
          break;
      }
    }
    public void updateData()
    {
      dataGridView1.Rows.Clear();
      for (int i = 0; i < 10; i++)
      {
        dataGridView1.Rows.Add(id_register[i], value_read_broker[i]);
      }
    }

    public async void subcribe()
    {
      for (int i = 0; i < 10; i++)
      {
        try
        {
          var topic_sub = new MqttTopicFilterBuilder()
              .WithTopic(id_register[i])
              .WithAtMostOnceQoS()
              .Build();

          await client.SubscribeAsync(topic_sub);
        }
        catch (Exception ex)
        {
          MessageBox.Show(ex.Message);
        }
      }
    }
  }
}
