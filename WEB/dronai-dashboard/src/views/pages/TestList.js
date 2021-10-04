import React, {useState, useEffect} from 'react';
//import {Text} from 'react-native';
// material-ui
import { DataGrid, useGridApiRef } from '@material-ui/data-grid';
import {makeStyles} from '@material-ui/styles';

import configData from '../../config';

import axios from 'axios';

// project imports
import SubCard from './../../ui-component/cards/SubCard';
import MainCard from './../../ui-component/cards/MainCard';
import SecondaryAction from './../../ui-component/cards/CardSecondaryAction';
import { gridSpacing } from './../../store/constant';

import imgA from './../../assets/images/logo.png';


//==============================|| TYPOGRAPHY ||==============================//

const columns = [
    {
        field: 'imgPath', headerName: 'Image', width: 300
        , renderCell: (params) => (
            //params.value

            <img src={`${params.value}`} width="100%" height="100%"></img>
            //<img src="/static/media/logo.011a3aea.png"></img>
        )
    },
    { field: 'date', headerName: 'Date', width: 100 },
    { field: 'droneId', headerName: 'DRONE ID', width: 170 },
    { field: 'id', headerName: 'ID', width: 100 },
    {
        field: 'detail', headerName: 'Detail', width: 250
        , renderCell: (params) => (
            `${params.value}`

        )
    }
]

const rows = [
    { "imgPath": "/static/media/logo.011a3aea.png", id: 1, detail: 'Hello, world!Hello, world!Hello, world!Hello, world!Hello, world!Hello, world!Hello, world!' },
    {
        "id": "d95a4dfd-248c-4938-a6ca-1c5be05a33de",
        "date": "2021-10-04T05:12:15.000Z",
        "droneId": "Drone_Test",
        "detail": "ADD API 테스트입니다",
        "imgPath": "./resources/images/file-1633324335122.jpg"
    },
    { imgPath: '/static/media/logo.011a3aea.png', id: 8, detail: 'Hello, world!2' },
    { imgPath: 'i', id: 9, detail: 'test2' },
    { imgPath: '/static/media/logo.011a3aea.png', id: 11, detail: 'Hello, world!3' },
    { imgPath: 'i', id: 23, detail: 'test3' },
    { imgPath: '/static/media/profile.664db376.jpg', id: 5, detail: 'test4' }
]

const useStyles = makeStyles({
    text: {
        wordBreak : 'break-all'
    }
})

const TestList = () => {
    var loading;
    let [testRows, setTestRows] = useState([]);
    let [isLoading, setIsLoading] = useState(false);
    useEffect(()=>{
        fetchData();
    },[]);
    let fetchData = ()=>{
        setIsLoading(true);
        axios.get(configData.API_SERVER + 'event/get').then((response) => {
            console.log(response.data.events);
            setIsLoading(false);
    
            // 여기다가
            setTestRows(response.data.events);
        });

    }
    
        return (
            <MainCard title="Test List" secondary={<SecondaryAction link="https://next.material-ui.com/system/typography/" />}>
                <div style={{ height: 910, width: '100%' }}>
                    {isLoading ? <div></div>: <DataGrid rows={testRows} columns={columns} rowHeight={150} pageSize={5} checkboxSelection  />}
                </div>
            </MainCard>
        );
    
};

export default TestList;
