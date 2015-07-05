void SCI_Send_Datas(UARTn uratn)
{
	static unsigned short int send_data[3][8] = { { 0 }, { 0 }, { 0 } };

	send_data[0][0] = (unsigned short int)(10);
	send_data[0][1] = (unsigned short int)(20);
	send_data[0][2] = (unsigned short int)(30);
	send_data[0][3] = (unsigned short int)(40);
	send_data[0][4] = (unsigned short int)(0);
	send_data[0][5] = (unsigned short int)(0);
	send_data[0][6] = (unsigned short int)(0);
	send_data[0][7] = (unsigned short int)(0);

	send_data[1][0] = (unsigned short int)(0);
	send_data[1][1] = (unsigned short int)(0);
	send_data[1][2] = (unsigned short int)(0);
	send_data[1][3] = (unsigned short int)(0);
	send_data[1][4] = (unsigned short int)(0);
	send_data[1][5] = (unsigned short int)(0);
	send_data[1][6] = (unsigned short int)(0);
	send_data[1][7] = (unsigned short int)(0);

	send_data[2][0] = (unsigned short int)(0);
	send_data[2][1] = (unsigned short int)(0);
	send_data[2][2] = (unsigned short int)(0);
	send_data[2][3] = (unsigned short int)(0);
	send_data[2][4] = (unsigned short int)(0);
	send_data[2][5] = (unsigned short int)(0);
	send_data[2][6] = (unsigned short int)(0);
	send_data[2][7] = (unsigned short int)(0);

	uart_putchar(uratn, 'S');
	uart_putchar(uratn, 'T');
	for (int i = 0; i < 3; i++)
	for (int j = 0; j < 8; j++)
	{
		uart_putchar(uratn, (unsigned char)(send_data[i][j] & 0x00ff));
		uart_putchar(uratn, (unsigned char)(send_data[i][j] >> 8u));
	}
}