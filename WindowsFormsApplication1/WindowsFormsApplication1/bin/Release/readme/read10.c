void SCI_Send_Datas()
{
	int i, j;
	static unsigned int send_data[3][8] = { { 0 }, { 0 }, { 0 } };

	send_data[0][0] = (unsigned int)(10);
	send_data[0][1] = (unsigned int)(20);
	send_data[0][2] = (unsigned int)(30);
	send_data[0][3] = (unsigned int)(40);
	send_data[0][4] = (unsigned int)(0);
	send_data[0][5] = (unsigned int)(0);
	send_data[0][6] = (unsigned int)(0);
	send_data[0][7] = (unsigned int)(0);

	send_data[1][0] = (unsigned int)(0);
	send_data[1][1] = (unsigned int)(0);
	send_data[1][2] = (unsigned int)(0);
	send_data[1][3] = (unsigned int)(0);
	send_data[1][4] = (unsigned int)(0);
	send_data[1][5] = (unsigned int)(0);
	send_data[1][6] = (unsigned int)(0);
	send_data[1][7] = (unsigned int)(0);

	send_data[2][0] = (unsigned int)(0);
	send_data[2][1] = (unsigned int)(0);
	send_data[2][2] = (unsigned int)(0);
	send_data[2][3] = (unsigned int)(0);
	send_data[2][4] = (unsigned int)(0);
	send_data[2][5] = (unsigned int)(0);
	send_data[2][6] = (unsigned int)(0);
	send_data[2][7] = (unsigned int)(0);

	while (AS1_SendChar('S'));
	while (AS1_SendChar('T'));
	for (i = 0; i < 3; i++)
	for (j = 0; j < 8; j++)
	{
		while (AS1_SendChar((unsigned char)(send_data[i][j] & 0x00ff)));
		while (AS1_SendChar((unsigned char)(send_data[i][j] >> 8u)));
	}
}