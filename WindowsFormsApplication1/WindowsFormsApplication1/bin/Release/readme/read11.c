//send ccd Data
void SCI_Send_CCD(int ccdNum)
{
	int i;
	if (ccdNum != 1 && ccdNum != 2)
		return;
	while (AS1_SendChar(255));
	while (AS1_SendChar(255));
	while (AS1_SendChar(ccdNum);//number
	for (int i = 0; i < 128; i++)
		if (ccdData1[i] == 255)
			while (AS1_SendChar(254));
		else
			while (AS1_SendChar(ccdData1[i]));
	if (ccdNum == 2)
		for (int i = 0; i < 128; i++)
			if (ccdData2[i] == 255)
				while (AS1_SendChar(254));
			else
				while (AS1_SendChar(ccdData2[i]));
}