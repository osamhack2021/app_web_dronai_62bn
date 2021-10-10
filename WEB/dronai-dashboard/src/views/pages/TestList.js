import React, {useState, useEffect} from 'react';
//import {Text} from 'react-native';
// material-ui
import PropTypes from 'prop-types';
import Typography from '@material-ui/core/Typography';
import Paper from '@material-ui/core/Paper';
import Popper from '@material-ui/core/Popper';

import { DataGrid, useGridApiRef } from '@material-ui/data-grid';
import {createStyles, makeStyles} from '@material-ui/styles';

import configData from '../../config';

import axios from 'axios';

// project imports
import SubCard from './../../ui-component/cards/SubCard';
import MainCard from './../../ui-component/cards/MainCard';
import SecondaryAction from './../../ui-component/cards/CardSecondaryAction';
import { gridSpacing } from './../../store/constant';

import imgA from './../../assets/images/logo.png';


//==============================|| TYPOGRAPHY ||==============================//

const useStyles = makeStyles(() =>
  createStyles({
    root: {
      alignItems: 'center',
      lineHeight: '24px',
      width: '100%',
      height: '100%',
      position: 'relative',
      display: 'flex',
      '& .cellValue': {
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
      },
    },
  }),
);

function isOverflown(element) {
  return (
    element.scrollHeight > element.clientHeight ||
    element.scrollWidth > element.clientWidth
  );
}

const GridCellExpand = React.memo(function GridCellExpand(props) {
  const { width, value } = props;
  const wrapper = React.useRef(null);
  const cellDiv = React.useRef(null);
  const cellValue = React.useRef(null);
  const [anchorEl, setAnchorEl] = React.useState(null);
  const classes = useStyles();
  const [showFullCell, setShowFullCell] = React.useState(false);
  const [showPopper, setShowPopper] = React.useState(false);

  const handleMouseEnter = () => {
    const isCurrentlyOverflown = isOverflown(cellValue.current);
    setShowPopper(isCurrentlyOverflown);
    setAnchorEl(cellDiv.current);
    setShowFullCell(true);
  };

  const handleMouseLeave = () => {
    setShowFullCell(false);
  };

  React.useEffect(() => {
    if (!showFullCell) {
      return undefined;
    }

    function handleKeyDown(nativeEvent) {
      // IE11, Edge (prior to using Bink?) use 'Esc'
      if (nativeEvent.key === 'Escape' || nativeEvent.key === 'Esc') {
        setShowFullCell(false);
      }
    }

    document.addEventListener('keydown', handleKeyDown);

    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [setShowFullCell, showFullCell]);

  return (
    <div
      ref={wrapper}
      className={classes.root}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
    >
      <div
        ref={cellDiv}
        style={{
          height: 1,
          width,
          display: 'block',
          position: 'absolute',
          top: 0,
        }}
      />
      <div ref={cellValue} className="cellValue">
        {value}
      </div>
      {showPopper && (
        <Popper
          open={showFullCell && anchorEl !== null}
          anchorEl={anchorEl}
          style={{ width, marginLeft: -17 }}
        >
          <Paper
            elevation={1}
            style={{ minHeight: wrapper.current.offsetHeight - 3 }}
          >
            <Typography variant="body2" style={{ padding: 8 }}>
              {value}
            </Typography>
          </Paper>
        </Popper>
      )}
    </div>
  );
});

GridCellExpand.propTypes = {
  value: PropTypes.string.isRequired,
  width: PropTypes.number.isRequired,
};

function renderCellExpand(params) {
  return (
    <GridCellExpand
      value={params.value ? params.value.toString() : ''}
      width={params.colDef.computedWidth}
    />
  );
}

renderCellExpand.propTypes = {
  /**
   * The column of the row that the current cell belongs to.
   */
  colDef: PropTypes.object.isRequired,
  /**
   * The cell value, but if the column has valueGetter, use getValue.
   */
  value: PropTypes.oneOfType([
    PropTypes.instanceOf(Date),
    PropTypes.number,
    PropTypes.object,
    PropTypes.string,
    PropTypes.bool,
  ]),
};


const columns = [
    {
        field: 'imgPath', headerName: 'Image', width: 300
        , renderCell: (params) => (
            //params.value

            <img src={`${params.value}`} width="100%" height="100%"></img>
            //<img src="/static/media/logo.011a3aea.png"></img>
        )
    },
    { field: 'date', headerName: 'Date', width: 120, renderCell: renderCellExpand, },
    { field: 'droneId', headerName: 'DRONE ID', width: 170, renderCell: renderCellExpand, },
    { field: 'id', headerName: 'ID', width: 120, renderCell: renderCellExpand, },
    {
        field: 'detail', headerName: 'Detail', width: 500
         , renderCell: renderCellExpand,
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
            <MainCard title="Event List" secondary={<SecondaryAction link="https://next.material-ui.com/system/typography/" />}>
                <div style={{ height: 910, width: '100%' }}>
                    {isLoading ? <div></div>: <DataGrid rows={testRows} columns={columns} rowHeight={150} pageSize={5} checkboxSelection  />}
                </div>
            </MainCard>
        );
    
};

export default TestList;
