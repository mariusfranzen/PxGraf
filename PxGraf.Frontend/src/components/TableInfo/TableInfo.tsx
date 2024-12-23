import { Link } from "react-router-dom";
import { useTranslation } from 'react-i18next';
import { ListItemText, ListItemButton, Typography, Divider, AlertTitle, Alert } from '@mui/material';
import React from 'react';
import { urls } from 'Router';
import { spacing } from 'utils/componentHelpers';
import UiLanguageContext from 'contexts/uiLanguageContext';
import { IDatabaseTable } from "../../api/services/table";
import { getErrorText } from "../../utils/editorHelpers";
import styled from "styled-components";

interface ITableInfoProps {
    path: string;
    item: IDatabaseTable;
}

const ErrorAlert = styled(Alert)`
  width: 100%;
`;

export const TableInfo: React.FC<ITableInfoProps> = ({ path, item }) => {
    const { t } = useTranslation();
    const { language } = React.useContext(UiLanguageContext);
    const currentPath = [path, item.code];
    const displayLanguage = item.languages.includes(language) ? language : item.languages[0];

    return (
        <>
            <ListItemButton id="mainContent" component={Link} to={urls.editor(currentPath)}>
                {item.error ?
                    <ErrorAlert severity="warning">
                        <AlertTitle>{`${item.name[displayLanguage] ?? item.code}`}</AlertTitle>
                        {getErrorText(item.error)}
                    </ErrorAlert>
                    :
                    <ListItemText primary={
                        <>
                            <Typography variant="body1" sx={{ ...spacing(1) }}>
                                {item.name[displayLanguage]}
                            </Typography>
                            <Typography variant="body2" sx={{ ...spacing(1) }}>
                                {t("tableSelect.updated") + ": " + new Date(item.lastUpdated).toLocaleString(language)}
                            </Typography>
                        </>
                    } />}
            </ListItemButton>
            <Divider />
        </>
    );
}

export default TableInfo;